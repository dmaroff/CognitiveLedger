using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CognitiveLedger.Agents;
using CognitiveLedger.Common;
using CognitiveLedger.Services.Agents.Api.Configuration;
using CognitiveLedger.Services.Agents.Api.Endpoints;
using CognitiveLedger.Services.Agents.Api.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Serilog;

namespace CognitiveLedger.Services.Agents.Api;

public static class Program
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> VisibleToolArgumentsByTool =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["search_transactions"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "accountName", "category", "fromDate", "isCredit", "issuer", "limit",
                "maximumAmount", "minimumAmount", "toDate"
            },
            ["summarize_transactions"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "accountName", "category", "fromDate", "groupBy", "groupLimit", "issuer",
                "maximumAmount", "minimumAmount", "toDate"
            },
            ["get_account_overview"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "accountName", "asOfDate", "issuer"
            }
        };

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddTransient(typeof(AppLog<>));

        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

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
        builder.Services.AddHttpClient("Ollama", (serviceProvider, httpClient) =>
        {
            var modelOptions = serviceProvider
                .GetRequiredService<IOptions<LocalModelOptions>>()
                .Value;

            httpClient.BaseAddress = modelOptions.Endpoint;
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
        });
        builder.Services.AddScoped<IChatClient>(CreateChatClient);
        builder.Services.AddSingleton<LedgerMcpToolProvider>();
        builder.Services.AddSingleton<IAgentToolProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<LedgerMcpToolProvider>());
        builder.Services.AddScoped<ILedgerAgent, LedgerAgent>();

        var app = builder.Build();

        // Optional but highly recommended: Automatically logs HTTP requests
        // This middleware automatically utilizes FromLogContext to attach request metadata!
        app.UseSerilogRequestLogging();

        app.MapAgentEndpoints();

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var urls = app.Urls.Count > 0
                ? string.Join(", ", app.Urls)
                : "unknown";

            app.Logger.LogInformation(
                "CognitiveLedger.Services.Agents.Api is listening at {Urls}",
                urls);
        });

        app.Run();
    }

    private static IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var modelOptions = serviceProvider.GetRequiredService<IOptions<LocalModelOptions>>().Value;
        var agentOptions = serviceProvider.GetRequiredService<IOptions<AgentApiOptions>>().Value;
        var executionRecorder = serviceProvider.GetRequiredService<IAgentToolExecutionRecorder>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = serviceProvider.GetRequiredService<AppLog<ChatClientBuilder>>();
        var ollamaHttpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("Ollama");
        IChatClient ollamaClient = new OllamaApiClient(
            ollamaHttpClient,
            modelOptions.ModelName);

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
        AppLog<ChatClientBuilder> logger,
        CancellationToken cancellationToken)
    {
        logger.LogMethodStart();
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var visibleArguments = GetVisibleToolArguments(
            context.Function.Name,
            context.Arguments);

        try
        {
            var result = await context.Function.InvokeAsync(
                context.Arguments,
                cancellationToken);

            executionRecorder.Record(new AgentToolExecution
            {
                Name = context.Function.Name,
                Arguments = visibleArguments,
                CallId = context.CallContent.CallId,
                Succeeded = !IsToolError(result),
                Iteration = context.Iteration,
                StartedAtUtc = startedAtUtc,
                Duration = stopwatch.Elapsed
            });

            logger.LogMethodEnd();
            return result;
        }
        catch (Exception exception)
        {
            executionRecorder.Record(new AgentToolExecution
            {
                Name = context.Function.Name,
                Arguments = visibleArguments,
                CallId = context.CallContent.CallId,
                Succeeded = false,
                ErrorMessage = "Tool execution failed.",
                Iteration = context.Iteration,
                StartedAtUtc = startedAtUtc,
                Duration = stopwatch.Elapsed
            });

            logger.LogError(
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

    private static IReadOnlyDictionary<string, string?> GetVisibleToolArguments(
        string toolName,
        AIFunctionArguments arguments)
    {
        if (!VisibleToolArgumentsByTool.TryGetValue(toolName, out var visibleArguments))
        {
            return new Dictionary<string, string?>();
        }

        return arguments
            .Where(argument => visibleArguments.Contains(argument.Key))
            .OrderBy(argument => argument.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                argument => argument.Key,
                argument => FormatToolArgument(argument.Value),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? FormatToolArgument(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.Null } => null,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            JsonElement json when json.ValueKind is
                JsonValueKind.Number or
                JsonValueKind.True or
                JsonValueKind.False => json.GetRawText(),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
