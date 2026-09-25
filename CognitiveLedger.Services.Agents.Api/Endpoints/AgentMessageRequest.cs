namespace CognitiveLedger.Services.Agents.Api.Endpoints;

public sealed class AgentMessageRequest
{
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
}
