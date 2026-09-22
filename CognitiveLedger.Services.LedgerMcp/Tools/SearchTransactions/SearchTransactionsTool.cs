using System.ComponentModel;
using CognitiveLedger.Services.LedgerMcp.Security;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

[McpServerToolType]
public static class SearchTransactionsTool
{
    private const int MaximumResultLimit = 100;

    [McpServerTool(
        Name = "search_transactions",
        Title = "Search transactions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description(
        "Search the current user's imported ledger transactions using optional merchant, " +
        "description, category, issuer, account, date, amount, and credit filters.")]
    public static Task<SearchTransactionsResult> SearchAsync(
        ICurrentUserContext currentUser,
        ITransactionSearchQuery transactionSearchQuery,
        [Description("Merchant name or partial merchant name.")]
        string? merchant = null,
        [Description("Text contained in the transaction description.")]
        string? description = null,
        [Description("Exact transaction category, matched without regard to case.")]
        string? category = null,
        [Description("Statement issuer or partial issuer name, such as Synchrony.")]
        string? issuer = null,
        [Description("Account name or partial account name, such as Amazon or Lowe's.")]
        string? accountName = null,
        [Description("Inclusive transaction start date.")]
        DateOnly? fromDate = null,
        [Description("Inclusive transaction end date.")]
        DateOnly? toDate = null,
        [Description("Inclusive minimum transaction amount.")]
        decimal? minimumAmount = null,
        [Description("Inclusive maximum transaction amount.")]
        decimal? maximumAmount = null,
        [Description("True for credits, false for charges, or omit for both.")]
        bool? isCredit = null,
        [Description("Maximum number of matching transactions to return, from 1 through 100.")]
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.TryGetUserId(out var userId))
        {
            throw new McpException("An authenticated user is required to search transactions.");
        }

        var resultLimit = limit ?? 25;

        if (resultLimit is < 1 or > MaximumResultLimit)
        {
            throw new McpException($"Limit must be between 1 and {MaximumResultLimit}.");
        }

        if (fromDate > toDate)
        {
            throw new McpException("FromDate cannot be after ToDate.");
        }

        if (minimumAmount > maximumAmount)
        {
            throw new McpException("MinimumAmount cannot be greater than MaximumAmount.");
        }

        return transactionSearchQuery.SearchAsync(
            userId,
            new SearchTransactionsArguments
            {
                Merchant = merchant,
                Description = description,
                Category = category,
                Issuer = issuer,
                AccountName = accountName,
                FromDate = fromDate,
                ToDate = toDate,
                MinimumAmount = minimumAmount,
                MaximumAmount = maximumAmount,
                IsCredit = isCredit,
                Limit = resultLimit
            },
            cancellationToken);
    }
}
