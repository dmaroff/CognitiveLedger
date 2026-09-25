using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

public sealed record SearchTransactionsResult
{
    public required int TotalMatches { get; init; }
    public required IReadOnlyList<TransactionSearchMatch> Transactions { get; init; }
}

public sealed class SearchTransactionsRequest : RequestBase
{
    public required SearchTransactionsArguments Arguments { get; init; }
}

public sealed record TransactionSearchMatch
{
    public required long TransactionId { get; init; }
    public required long StatementId { get; init; }
    public DateOnly? TransactionDate { get; init; }
    public required string Merchant { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required decimal Amount { get; init; }
    public required bool IsCredit { get; init; }
    public required string Issuer { get; init; }
    public required string AccountName { get; init; }
}
