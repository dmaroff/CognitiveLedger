namespace CognitiveLedger.Services.Agents.Api.Endpoints;

public sealed record AgentMessageRequest
{
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
}
