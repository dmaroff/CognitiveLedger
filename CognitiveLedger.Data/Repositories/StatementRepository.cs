using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Data.Repositories;

public sealed class StatementRepository(CognitiveLedgerDbContext dbContext)
    : IStatementRepository
{
    public async Task<CreditCardStatement?> FindBySourceDocumentSha256Async(
        long userId,
        string sourceDocumentSha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDocumentSha256);

        return await dbContext.Statements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                statement => statement.UserId == userId &&
                             statement.SourceDocumentSha256 == sourceDocumentSha256,
                cancellationToken);
    }

    public async Task<CreditCardStatement> InsertStatementAsync(
        CreditCardStatement statement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statement);

        if (statement.UserId <= 0)
        {
            throw new ArgumentException(
                "A statement must belong to a user.",
                nameof(statement));
        }

        if (string.IsNullOrWhiteSpace(statement.SourceDocumentSha256) ||
            statement.SourceDocumentSha256.Length != 64)
        {
            throw new ArgumentException(
                "A statement must have a 64-character source document SHA-256 hash.",
                nameof(statement));
        }

        if (statement.Id != 0)
        {
            throw new ArgumentException(
                "A new statement cannot already have a database identifier.",
                nameof(statement));
        }

        if (statement.Transactions.Count == 0)
        {
            throw new ArgumentException(
                "A statement must contain at least one transaction.",
                nameof(statement));
        }

        var transactions = statement.Transactions.ToList();
        var createdAtUtc = DateTime.UtcNow;
        statement.CreatedAtUtc = createdAtUtc;

        foreach (var item in transactions.Select((transaction, index) =>
                     new { Transaction = transaction, Sequence = index + 1 }))
        {
            if (item.Transaction.Id != 0)
            {
                throw new ArgumentException(
                    "A new transaction cannot already have a database identifier.",
                    nameof(statement));
            }
            
            item.Transaction.Statement = statement;
            item.Transaction.Sequence = item.Sequence;
            item.Transaction.CreatedAtUtc = createdAtUtc;
        }

        var duplicateSequences = transactions
            .GroupBy(transaction => transaction.Sequence)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateSequences.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate transaction sequences were assigned: " +
                $"{string.Join(", ", duplicateSequences)}.");
        }

        // Insert in two explicit phases so the generated statement ID is known before
        // transaction rows are sent to PostgreSQL. The surrounding transaction keeps the
        // statement and all of its transactions atomic.
        statement.Transactions.Clear();
        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Statements.AddAsync(statement, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.StatementId = statement.Id;
            transaction.Statement = statement;
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return statement;
    }
}
