namespace CognitiveLedger.Services.Agents.Api.Configuration;

public sealed class LedgerMcpOptions
{
    public const string SectionName = "LedgerMcp";

    public required Uri Endpoint { get; init; }
}
