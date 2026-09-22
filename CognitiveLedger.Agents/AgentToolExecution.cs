namespace CognitiveLedger.Agents;

public sealed record AgentToolExecution
{
    public required string Name { get; init; }
    public string? CallId { get; init; }
    public bool? Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
}
