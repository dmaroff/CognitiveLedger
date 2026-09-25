namespace CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;

public sealed record GetAccountOverviewArguments
{
    public string? Issuer { get; init; }
    public string? AccountName { get; init; }
    public DateOnly? AsOfDate { get; init; }
}
