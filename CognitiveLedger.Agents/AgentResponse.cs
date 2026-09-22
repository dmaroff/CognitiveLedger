namespace CognitiveLedger.Agents;

public sealed record AgentResponse
{
    public required string Answer { get; init; }
    public string? ConversationId { get; init; }
    public IReadOnlyList<AgentToolExecution> ToolExecutions { get; init; } = [];
}
