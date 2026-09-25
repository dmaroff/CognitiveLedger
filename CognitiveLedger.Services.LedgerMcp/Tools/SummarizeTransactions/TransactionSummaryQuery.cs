using System.Linq.Expressions;
using CognitiveLedger.Common;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;

public sealed class TransactionSummaryQuery : ITransactionSummaryQuery
{
    private readonly CognitiveLedgerDbContext _dbContext;
    private readonly AppLog<TransactionSummaryQuery> _logger;

    public TransactionSummaryQuery(
        CognitiveLedgerDbContext dbContext,
        AppLog<TransactionSummaryQuery> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<SummarizeTransactionsResult> SummarizeAsync(
        SummarizeTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogMethodStart(request);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.UserId);
        ArgumentNullException.ThrowIfNull(request.Arguments);

        _logger.LogInfo(request, $"Applying filters");
        var query = ApplyFilters(
            request, _dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.Statement.UserId == request.UserId),
            request.Arguments);

        _logger.LogInfo(request, $"Calculating totals");
        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new AggregateProjection
            {
                TransactionCount = group.Count(),
                ChargeCount = group.Count(transaction => !transaction.IsCredit),
                CreditCount = group.Count(transaction => transaction.IsCredit),
                TotalCharges = group.Sum(transaction =>
                    transaction.IsCredit ? 0m : transaction.Amount),
                TotalCredits = group.Sum(transaction =>
                    transaction.IsCredit ? transaction.Amount : 0m),
                NetSpending = group.Sum(transaction =>
                    transaction.IsCredit ? -transaction.Amount : transaction.Amount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        _logger.LogInfo(request, $"Calculating groups");
        var groupResult = await GetGroupsAsync(
            request,
            query,
            request.Arguments.GroupBy,
            request.Arguments.GroupLimit,
            cancellationToken);

        var result = new SummarizeTransactionsResult
        {
            TransactionCount = totals?.TransactionCount ?? 0,
            ChargeCount = totals?.ChargeCount ?? 0,
            CreditCount = totals?.CreditCount ?? 0,
            TotalCharges = totals?.TotalCharges ?? 0m,
            TotalCredits = totals?.TotalCredits ?? 0m,
            NetSpending = totals?.NetSpending ?? 0m,
            GroupedBy = request.Arguments.GroupBy,
            TotalGroups = groupResult.TotalGroups,
            Groups = groupResult.Groups
        };

        _logger.LogMethodEnd(request);
        return result;
    }

    private IQueryable<CreditCardTransaction> ApplyFilters(
        SummarizeTransactionsRequest request,
        IQueryable<CreditCardTransaction> query,
        SummarizeTransactionsArguments arguments)
    {
        _logger.LogInfo(request, $"Applying filter for merchant: {arguments.Merchant}");
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

        return query;
    }

    private async Task<GroupResult> GetGroupsAsync(
        SummarizeTransactionsRequest request,
        IQueryable<CreditCardTransaction> query,
        string? groupBy,
        int groupLimit,
        CancellationToken cancellationToken)
    {
        if (groupBy is null)
        {
            _logger.LogInfo(request, $"No grouping specified");
            return new GroupResult(0, []);
        }

        _logger.LogInfo(request, $"Grouping transactions by '{groupBy}'");
        return groupBy switch
        {
            "category" => await GetTextGroupsAsync(
                query,
                transaction => transaction.Category,
                groupLimit,
                cancellationToken),
            "merchant" => await GetTextGroupsAsync(
                query,
                transaction => transaction.Merchant,
                groupLimit,
                cancellationToken),
            "account" => await GetTextGroupsAsync(
                query,
                transaction => transaction.Statement.AccountName,
                groupLimit,
                cancellationToken),
            "issuer" => await GetTextGroupsAsync(
                query,
                transaction => transaction.Statement.Issuer,
                groupLimit,
                cancellationToken),
            "month" => await GetMonthGroupsAsync(
                query,
                groupLimit,
                cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported transaction grouping '{groupBy}'.")
        };
    }

    private static async Task<GroupResult> GetTextGroupsAsync(
        IQueryable<CreditCardTransaction> query,
        Expression<Func<CreditCardTransaction, string>> keySelector,
        int groupLimit,
        CancellationToken cancellationToken)
    {
        var groupedQuery = query
            .GroupBy(keySelector)
            .Select(group => new TextGroupProjection
            {
                Key = group.Key,
                TransactionCount = group.Count(),
                ChargeCount = group.Count(transaction => !transaction.IsCredit),
                CreditCount = group.Count(transaction => transaction.IsCredit),
                TotalCharges = group.Sum(transaction =>
                    transaction.IsCredit ? 0m : transaction.Amount),
                TotalCredits = group.Sum(transaction =>
                    transaction.IsCredit ? transaction.Amount : 0m),
                NetSpending = group.Sum(transaction =>
                    transaction.IsCredit ? -transaction.Amount : transaction.Amount)
            });

        var totalGroups = await groupedQuery.CountAsync(cancellationToken);
        var projections = await groupedQuery
            .OrderByDescending(group => group.NetSpending)
            .ThenBy(group => group.Key)
            .Take(groupLimit)
            .ToArrayAsync(cancellationToken);

        return new GroupResult(
            totalGroups,
            [
                .. projections.Select(group => ToResultGroup(
                    string.IsNullOrWhiteSpace(group.Key) ? "(unknown)" : group.Key,
                    group))
            ]);
    }

    private static async Task<GroupResult> GetMonthGroupsAsync(
        IQueryable<CreditCardTransaction> query,
        int groupLimit,
        CancellationToken cancellationToken)
    {
        var groupedQuery = query
            .GroupBy(transaction => new
            {
                Year = transaction.TransactionDate == null
                    ? (int?)null
                    : transaction.TransactionDate.Value.Year,
                Month = transaction.TransactionDate == null
                    ? (int?)null
                    : transaction.TransactionDate.Value.Month
            })
            .Select(group => new MonthGroupProjection
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TransactionCount = group.Count(),
                ChargeCount = group.Count(transaction => !transaction.IsCredit),
                CreditCount = group.Count(transaction => transaction.IsCredit),
                TotalCharges = group.Sum(transaction =>
                    transaction.IsCredit ? 0m : transaction.Amount),
                TotalCredits = group.Sum(transaction =>
                    transaction.IsCredit ? transaction.Amount : 0m),
                NetSpending = group.Sum(transaction =>
                    transaction.IsCredit ? -transaction.Amount : transaction.Amount)
            });

        var totalGroups = await groupedQuery.CountAsync(cancellationToken);
        var projections = await groupedQuery
            .OrderByDescending(group => group.NetSpending)
            .ThenByDescending(group => group.Year)
            .ThenByDescending(group => group.Month)
            .Take(groupLimit)
            .ToArrayAsync(cancellationToken);

        return new GroupResult(
            totalGroups,
            [
                .. projections.Select(group => ToResultGroup(
                    group.Year is null || group.Month is null
                        ? "(unknown)"
                        : $"{group.Year:D4}-{group.Month:D2}",
                    group))
            ]);
    }

    private static TransactionSummaryGroup ToResultGroup(
        string key,
        AggregateProjection projection) =>
        new()
        {
            Key = key,
            TransactionCount = projection.TransactionCount,
            ChargeCount = projection.ChargeCount,
            CreditCount = projection.CreditCount,
            TotalCharges = projection.TotalCharges,
            TotalCredits = projection.TotalCredits,
            NetSpending = projection.NetSpending
        };

    private static string ContainsPattern(string value) =>
        $"%{EscapeLikePattern(value.Trim())}%";

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", @"\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record GroupResult(
        int TotalGroups,
        IReadOnlyList<TransactionSummaryGroup> Groups);

    private class AggregateProjection
    {
        public int TransactionCount { get; init; }
        public int ChargeCount { get; init; }
        public int CreditCount { get; init; }
        public decimal TotalCharges { get; init; }
        public decimal TotalCredits { get; init; }
        public decimal NetSpending { get; init; }
    }

    private sealed class TextGroupProjection : AggregateProjection
    {
        public required string Key { get; init; }
    }

    private sealed class MonthGroupProjection : AggregateProjection
    {
        public int? Year { get; init; }
        public int? Month { get; init; }
    }
}