namespace CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;

public interface IAccountOverviewQuery
{
    Task<GetAccountOverviewResult> GetAsync(
        GetAccountOverviewRequest request,
        CancellationToken cancellationToken = default);
}
