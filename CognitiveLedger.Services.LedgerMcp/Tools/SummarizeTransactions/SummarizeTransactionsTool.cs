using System.ComponentModel;
using CognitiveLedger.Services.LedgerMcp.Security;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;

[McpServerToolType]
public static class SummarizeTransactionsTool
{
    private const int MaximumGroupLimit = 50;

    [McpServerTool(
        Name = "summarize_transactions",
        Title = "Summarize transactions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description(
        "Calculate authoritative charge, credit, and net-spending totals for the current user's " +
        "transactions. Optionally group the summary by category, merchant, account, issuer, or month.")]
    public static Task<SummarizeTransactionsResult> SummarizeAsync(
        ICurrentUserContext currentUser,
        ITransactionSummaryQuery transactionSummaryQuery,
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
        [Description("Optional grouping: category, merchant, account, issuer, or month.")]
        string? groupBy = null,
        [Description("Maximum number of groups to return, from 1 through 50. Defaults to 10.")]
        int? groupLimit = null,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.TryGetUserId(out var userId))
        {
            throw new McpException("An authenticated user is required to summarize transactions.");
        }

        if (fromDate > toDate)
        {
            throw new McpException("FromDate cannot be after ToDate.");
        }

        if (minimumAmount > maximumAmount)
        {
            throw new McpException("MinimumAmount cannot be greater than MaximumAmount.");
        }

        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var resultGroupLimit = groupLimit ?? 10;

        if (resultGroupLimit is < 1 or > MaximumGroupLimit)
        {
            throw new McpException($"GroupLimit must be between 1 and {MaximumGroupLimit}.");
        }

        return transactionSummaryQuery.SummarizeAsync(
            new SummarizeTransactionsRequest
            {
                UserId = checked((int)userId),
                Arguments = new SummarizeTransactionsArguments
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
                    GroupBy = normalizedGroupBy,
                    GroupLimit = resultGroupLimit
                }
            },
            cancellationToken);
    }

    private static string? NormalizeGroupBy(string? groupBy)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
        {
            return null;
        }

        var normalized = groupBy.Trim().ToLowerInvariant();
        normalized = normalized == "accountname" ? "account" : normalized;

        return normalized is "category" or "merchant" or "account" or "issuer" or "month"
            ? normalized
            : throw new McpException(
                "GroupBy must be category, merchant, account, issuer, month, or omitted.");
    }
}
