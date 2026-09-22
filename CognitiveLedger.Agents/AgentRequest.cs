namespace CognitiveLedger.Agents;

public sealed record AgentRequest
{
    public required long UserId { get; init; }
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
}
