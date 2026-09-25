namespace CognitiveLedger.Agents;

public sealed record AgentToolExecution
{
    public required string Name { get; init; }
    public IReadOnlyDictionary<string, string?> Arguments { get; init; } =
        new Dictionary<string, string?>();
    public string? CallId { get; init; }
    public bool? Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public int Iteration { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public TimeSpan Duration { get; init; }
}
