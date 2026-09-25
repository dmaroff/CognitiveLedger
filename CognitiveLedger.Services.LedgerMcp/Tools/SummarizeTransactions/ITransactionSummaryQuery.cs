namespace CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;

public interface ITransactionSummaryQuery
{
    /// <summary>
    /// Summarizes the transactions for a given user based on the provided arguments.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SummarizeTransactionsResult> SummarizeAsync(
        SummarizeTransactionsRequest request,
        CancellationToken cancellationToken = default);
}