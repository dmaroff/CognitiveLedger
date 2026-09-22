namespace CognitiveLedger.Services.Agents.Api.Configuration;

public sealed class AgentApiOptions
{
    public const string SectionName = "Agent";

    public long DevelopmentUserId { get; init; } = 1;
    public int RequestTimeoutSeconds { get; init; } = 120;
    public int MaximumToolIterations { get; init; } = 8;
}
