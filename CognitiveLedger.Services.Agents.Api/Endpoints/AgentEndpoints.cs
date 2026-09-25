using CognitiveLedger.Agents;
using CognitiveLedger.Services.Agents.Api.Configuration;
using Microsoft.Extensions.Options;
using OllamaSharp.Models.Exceptions;

namespace CognitiveLedger.Services.Agents.Api.Endpoints;


public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/agent/messages", SendMessageAsync)
            .WithName("SendAgentMessage");

        return endpoints;
    }

    private static async Task<IResult> SendMessageAsync(
        AgentMessageRequest request,
        ILedgerAgent agent,
        IOptions<AgentApiOptions> options,
        ILoggerFactory loggerFactory,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new { error = "A message is required." });
        }

        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds));
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        try
        {
            var response = await agent.RunAsync(
                new AgentRequest
                {
                    UserId = (int)options.Value.DevelopmentUserId,
                    Message = request.Message,
                    ConversationId = request.ConversationId
                },
                linkedCts.Token);

            return Results.Ok(response);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return Results.Problem(
                title: "The agent request timed out.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception exception) when (exception is HttpRequestException or OllamaException)
        {
            loggerFactory
                .CreateLogger("CognitiveLedger.Services.Agents.Api.AgentEndpoints")
                .LogWarning(exception, "An agent dependency is unavailable.");

            return Results.Problem(
                title: "An agent dependency is unavailable.",
                detail: "Verify that Ollama and Ledger MCP are running and reachable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
