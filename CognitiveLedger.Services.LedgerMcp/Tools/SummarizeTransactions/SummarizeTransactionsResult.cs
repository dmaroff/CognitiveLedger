using CognitiveLedger.Common.Request;
using CognitiveLedger.Common.Response;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;

public sealed class SummarizeTransactionsRequest : RequestBase
{
    public required SummarizeTransactionsArguments Arguments { get; init; }
}

public sealed class SummarizeTransactionsResult : ResponseBase
{
    public required int TransactionCount { get; init; }
    public required int ChargeCount { get; init; }
    public required int CreditCount { get; init; }
    public required decimal TotalCharges { get; init; }
    public required decimal TotalCredits { get; init; }
    public required decimal NetSpending { get; init; }
    public string? GroupedBy { get; init; }
    public required int TotalGroups { get; init; }
    public required IReadOnlyList<TransactionSummaryGroup> Groups { get; init; }
}

public sealed class TransactionSummaryGroup : ResponseBase
{
    public required string Key { get; init; }
    public required int TransactionCount { get; init; }
    public required int ChargeCount { get; init; }
    public required int CreditCount { get; init; }
    public required decimal TotalCharges { get; init; }
    public required decimal TotalCredits { get; init; }
    public required decimal NetSpending { get; init; }
}