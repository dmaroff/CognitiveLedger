using CognitiveLedger.Common;
using CognitiveLedger.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;

public sealed class TransactionSearchQuery : ITransactionSearchQuery
{

    private readonly CognitiveLedgerDbContext _dbContext;
    private readonly AppLog<TransactionSearchQuery> _logger;

    public TransactionSearchQuery(
        CognitiveLedgerDbContext dbContext,
        AppLog<TransactionSearchQuery> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<SearchTransactionsResult> SearchAsync(
        SearchTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogMethodStart(request);
        var (userId, arguments) = (request.UserId, request.Arguments);
        
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentNullException.ThrowIfNull(arguments);

        var query = _dbContext.Transactions
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

        _logger.LogMethodEnd(request);
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
