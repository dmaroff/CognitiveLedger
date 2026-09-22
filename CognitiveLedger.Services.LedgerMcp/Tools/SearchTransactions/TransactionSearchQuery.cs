using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

public sealed class TransactionSearchQuery(CognitiveLedgerDbContext dbContext)
    : ITransactionSearchQuery
{
    public async Task<SearchTransactionsResult> SearchAsync(
        long userId,
        SearchTransactionsArguments arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentNullException.ThrowIfNull(arguments);

        var query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Statement.UserId == userId);

        if (!string.IsNullOrWhiteSpace(arguments.Merchant))
        {
            var pattern = ContainsPattern(arguments.Merchant);
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Merchant, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(arguments.Description))
        {
            var pattern = ContainsPattern(arguments.Description);
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Description, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(arguments.Category))
        {
            var category = EscapeLikePattern(arguments.Category.Trim());
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Category, category, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(arguments.Issuer))
        {
            var pattern = ContainsPattern(arguments.Issuer);
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Statement.Issuer, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(arguments.AccountName))
        {
            var pattern = ContainsPattern(arguments.AccountName);
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Statement.AccountName, pattern, "\\"));
        }

        if (arguments.FromDate is not null)
        {
            query = query.Where(transaction =>
                transaction.TransactionDate >= arguments.FromDate);
        }

        if (arguments.ToDate is not null)
        {
            query = query.Where(transaction =>
                transaction.TransactionDate <= arguments.ToDate);
        }

        if (arguments.MinimumAmount is not null)
        {
            query = query.Where(transaction =>
                transaction.Amount >= arguments.MinimumAmount);
        }

        if (arguments.MaximumAmount is not null)
        {
            query = query.Where(transaction =>
                transaction.Amount <= arguments.MaximumAmount);
        }

        if (arguments.IsCredit is not null)
        {
            query = query.Where(transaction =>
                transaction.IsCredit == arguments.IsCredit);
        }

        var totalMatches = await query.CountAsync(cancellationToken);
        var transactions = await query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .Take(arguments.Limit)
            .Select(transaction => new TransactionSearchMatch
            {
                TransactionId = transaction.Id,
                StatementId = transaction.StatementId,
                TransactionDate = transaction.TransactionDate,
                Merchant = transaction.Merchant,
                Description = transaction.Description,
                Category = transaction.Category,
                Amount = transaction.Amount,
                IsCredit = transaction.IsCredit,
                Issuer = transaction.Statement.Issuer,
                AccountName = transaction.Statement.AccountName
            })
            .ToArrayAsync(cancellationToken);

        return new SearchTransactionsResult
        {
            TotalMatches = totalMatches,
            Transactions = transactions
        };
    }

    private static string ContainsPattern(string value) =>
        $"%{EscapeLikePattern(value.Trim())}%";

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
