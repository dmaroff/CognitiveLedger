namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

public interface ITransactionSearchQuery
{
    Task<SearchTransactionsResult> SearchAsync(
        long userId,
        SearchTransactionsArguments arguments,
        CancellationToken cancellationToken = default);
}
