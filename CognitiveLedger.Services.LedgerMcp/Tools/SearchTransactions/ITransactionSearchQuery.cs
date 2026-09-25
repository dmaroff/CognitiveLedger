namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

public interface ITransactionSearchQuery
{
    /// <summary>
    /// Searches the transactions for a given user based on the provided arguments.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SearchTransactionsResult> SearchAsync(
        SearchTransactionsRequest request,
        CancellationToken cancellationToken = default);
}