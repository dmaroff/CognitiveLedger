using CognitiveLedger.Common.Request;
using CognitiveLedger.Common.Response;

namespace CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;

public sealed class GetAccountOverviewRequest : RequestBase
{
    public required GetAccountOverviewArguments Arguments { get; init; }
}

public sealed class GetAccountOverviewResult : ResponseBase
{
    public required int AccountCount { get; init; }
    public required decimal TotalLatestStatementBalance { get; init; }
    public required IReadOnlyList<AccountStatementSnapshot> Accounts { get; init; }
}

public sealed class AccountStatementSnapshot
{
    public required string Issuer { get; init; }
    public required string AccountName { get; init; }
    public required long StatementId { get; init; }
    public required DateOnly StatementPeriodStart { get; init; }
    public required DateOnly StatementPeriodEnd { get; init; }
    public required decimal PreviousBalance { get; init; }
    public required decimal LatestStatementBalance { get; init; }
    public required decimal TotalPurchases { get; init; }
    public required decimal TotalPayments { get; init; }
    public required decimal TotalOtherCredits { get; init; }
    public required decimal Fees { get; init; }
    public required decimal InterestCharged { get; init; }
}
