using CognitiveLedger.Common;
using CognitiveLedger.Data.Database;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;

public sealed class AccountOverviewQuery(
    CognitiveLedgerDbContext dbContext,
    AppLog<AccountOverviewQuery> logger) : IAccountOverviewQuery
{
    public async Task<GetAccountOverviewResult> GetAsync(
        GetAccountOverviewRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogMethodStart(request);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.UserId);
        ArgumentNullException.ThrowIfNull(request.Arguments);

        var statements = dbContext.Statements
            .AsNoTracking()
            .Where(statement => statement.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Arguments.Issuer))
        {
            var pattern = ContainsPattern(request.Arguments.Issuer);
            statements = statements.Where(statement =>
                EF.Functions.ILike(statement.Issuer, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(request.Arguments.AccountName))
        {
            var pattern = ContainsPattern(request.Arguments.AccountName);
            statements = statements.Where(statement =>
                EF.Functions.ILike(statement.AccountName, pattern, "\\"));
        }

        if (request.Arguments.AsOfDate is not null)
        {
            statements = statements.Where(statement =>
                statement.StatementPeriodEnd <= request.Arguments.AsOfDate);
        }

        var latestStatements = statements.Where(statement =>
            !statements.Any(candidate =>
                candidate.Issuer == statement.Issuer &&
                candidate.AccountName == statement.AccountName &&
                (candidate.StatementPeriodEnd > statement.StatementPeriodEnd ||
                 candidate.StatementPeriodEnd == statement.StatementPeriodEnd &&
                 candidate.Id > statement.Id)));

        var accounts = await latestStatements
            .Select(statement => new AccountStatementSnapshot
            {
                Issuer = statement.Issuer,
                AccountName = statement.AccountName,
                StatementId = statement.Id,
                StatementPeriodStart = statement.StatementPeriodStart,
                StatementPeriodEnd = statement.StatementPeriodEnd,
                PreviousBalance = statement.PreviousBalance,
                LatestStatementBalance = statement.NewBalance,
                TotalPurchases = statement.TotalPurchases,
                TotalPayments = statement.TotalPayments,
                TotalOtherCredits = statement.TotalOtherCredits,
                Fees = statement.Fees,
                InterestCharged = statement.InterestCharged
            })
            .OrderBy(account => account.Issuer)
            .ThenBy(account => account.AccountName)
            .ToArrayAsync(cancellationToken);

        var result = new GetAccountOverviewResult
        {
            AccountCount = accounts.Length,
            TotalLatestStatementBalance = accounts.Sum(account =>
                account.LatestStatementBalance),
            Accounts = accounts
        };

        logger.LogMethodEnd(request);
        return result;
    }

    private static string ContainsPattern(string value) =>
        $"%{EscapeLikePattern(value.Trim())}%";

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
