using System.Diagnostics;
using System.Text.Json;
using CognitiveLedger.Agents;
using CognitiveLedger.Services.Agents.Api.Configuration;
using CognitiveLedger.Services.Agents.Api.Endpoints;
using CognitiveLedger.Services.Agents.Api.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace CognitiveLedger.Services.Agents.Api;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddOptions<AgentApiOptions>()
            .Bind(builder.Configuration.GetSection(AgentApiOptions.SectionName))
            .Validate(options => options.DevelopmentUserId > 0, "DevelopmentUserId must be positive.")
            .Validate(options => options.RequestTimeoutSeconds > 0, "RequestTimeoutSeconds must be positive.")
            .Validate(options => options.MaximumToolIterations > 0, "MaximumToolIterations must be positive.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<LedgerMcpOptions>()
            .Bind(builder.Configuration.GetSection(LedgerMcpOptions.SectionName))
            .Validate(options => options.Endpoint is { IsAbsoluteUri: true }, "LedgerMcp:Endpoint must be an absolute URI.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<LocalModelOptions>()
            .Bind(builder.Configuration.GetSection(LocalModelOptions.SectionName))
            .Validate(options => options.Endpoint is { IsAbsoluteUri: true }, "Ollama:Endpoint must be an absolute URI.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ModelName), "ModelName is required.")
            .ValidateOnStart();

        builder.Services.AddScoped<IAgentToolExecutionRecorder, AgentToolExecutionRecorder>();
        builder.Services.AddScoped<IChatClient>(CreateChatClient);
        builder.Services.AddSingleton<LedgerMcpToolProvider>();
        builder.Services.AddSingleton<IAgentToolProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<LedgerMcpToolProvider>());
        builder.Services.AddScoped<ILedgerAgent, LedgerAgent>();

        var app = builder.Build();

        app.MapAgentEndpoints();

        app.Run();
    }

    private static IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var modelOptions = serviceProvider.GetRequiredService<IOptions<LocalModelOptions>>().Value;
        var agentOptions = serviceProvider.GetRequiredService<IOptions<AgentApiOptions>>().Value;
        var executionRecorder = serviceProvider.GetRequiredService<IAgentToolExecutionRecorder>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("CognitiveLedger.Agents.ToolInvocation");
        IChatClient ollamaClient = new OllamaApiClient(modelOptions.Endpoint, modelOptions.ModelName);

        return new ChatClientBuilder(ollamaClient)
            .UseFunctionInvocation(loggerFactory, functionOptions =>
            {
                functionOptions.MaximumIterationsPerRequest = agentOptions.MaximumToolIterations;
                functionOptions.MaximumConsecutiveErrorsPerRequest = 1;
                functionOptions.AllowConcurrentInvocation = false;
                functionOptions.FunctionInvoker = (context, cancellationToken) =>
                    InvokeFunctionAsync(
                        context,
                        executionRecorder,
                        logger,
                        cancellationToken);
            })
            .Build(serviceProvider);
    }

    private static async ValueTask<object?> InvokeFunctionAsync(
        FunctionInvocationContext context,
        IAgentToolExecutionRecorder executionRecorder,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await context.Function.InvokeAsync(
                context.Arguments,
                cancellationToken);

            executionRecorder.Record(new AgentToolExecution
            {
                Name = context.Function.Name,
                CallId = context.CallContent.CallId,
                Succeeded = !IsToolError(result),
                Iteration = context.Iteration,
                StartedAtUtc = startedAtUtc,
                Duration = stopwatch.Elapsed
            });

            return result;
        }
        catch (Exception exception)
        {
            executionRecorder.Record(new AgentToolExecution
            {
                Name = context.Function.Name,
                CallId = context.CallContent.CallId,
                Succeeded = false,
                ErrorMessage = "Tool execution failed.",
                Iteration = context.Iteration,
                StartedAtUtc = startedAtUtc,
                Duration = stopwatch.Elapsed
            });

            logger.LogWarning(
                exception,
                "Tool {ToolName} failed during agent iteration {Iteration}.",
                context.Function.Name,
                context.Iteration);

            throw;
        }
    }

    private static bool IsToolError(object? result)
    {
        return result is JsonElement { ValueKind: JsonValueKind.Object } json &&
               json.TryGetProperty("isError", out var isError) &&
               isError.ValueKind is JsonValueKind.True;
    }
}
