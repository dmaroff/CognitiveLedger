using CognitiveLedger.Agents;
using CognitiveLedger.Services.Agents.Api.Configuration;
using CognitiveLedger.Services.Agents.Api.Endpoints;
using CognitiveLedger.Services.Agents.Api.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

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

builder.Services.AddSingleton<IChatClient>(serviceProvider =>
{
    var modelOptions = serviceProvider.GetRequiredService<IOptions<LocalModelOptions>>().Value;
    var agentOptions = serviceProvider.GetRequiredService<IOptions<AgentApiOptions>>().Value;
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    IChatClient ollamaClient = new OllamaApiClient(modelOptions.Endpoint, modelOptions.ModelName);

    return new ChatClientBuilder(ollamaClient)
        .UseFunctionInvocation(loggerFactory, functionOptions =>
        {
            functionOptions.MaximumIterationsPerRequest = agentOptions.MaximumToolIterations;
            functionOptions.MaximumConsecutiveErrorsPerRequest = 1;
            functionOptions.AllowConcurrentInvocation = false;
        })
        .Build(serviceProvider);
});

builder.Services.AddSingleton<LedgerMcpToolProvider>();
builder.Services.AddSingleton<IAgentToolProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<LedgerMcpToolProvider>());
builder.Services.AddSingleton<ILedgerAgent, LedgerAgent>();

var app = builder.Build();

app.MapAgentEndpoints();

app.Run();
