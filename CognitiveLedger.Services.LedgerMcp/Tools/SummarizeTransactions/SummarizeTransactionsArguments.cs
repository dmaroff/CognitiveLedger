namespace CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;

public sealed record SummarizeTransactionsArguments
{
    public string? Merchant { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Issuer { get; init; }
    public string? AccountName { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public decimal? MinimumAmount { get; init; }
    public decimal? MaximumAmount { get; init; }
    public string? GroupBy { get; init; }
    public int GroupLimit { get; init; } = 10;
}
