namespace CognitiveLedger.Agents;

public sealed record AgentEvent
{
    public required AgentEventType Type { get; init; }
    public string? Content { get; init; }
    public AgentToolExecution? Tool { get; init; }
    public AgentResponse? Response { get; init; }
}

public enum AgentEventType
{
    MessageDelta,
    ToolCallStarted,
    ToolCallCompleted,
    Completed,
    Error
}
