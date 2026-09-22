using System;
using System.Threading;
using System.Threading.Tasks;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Models;
using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Data.Repositories;

public sealed class StatementProcessingRepository(CognitiveLedgerDbContext dbContext)
    : IStatementProcessingRepository
{
    private const int MaximumErrorMessageLength = 4000;

    public async Task<StatementProcessingAudit> StartAsync(
        StatementProcessingAudit audit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audit);

        if (audit.UserId <= 0)
        {
            throw new ArgumentException(
                "A processing audit must belong to a user.",
                nameof(audit));
        }

        if (audit.Id != 0)
        {
            throw new ArgumentException(
                "A new processing audit cannot already have a database identifier.",
                nameof(audit));
        }

        var startedAtUtc = DateTime.UtcNow;
        audit.StartedAtUtc = startedAtUtc;
        audit.CompletedAtUtc = null;
        audit.DurationMilliseconds = null;
        audit.StatusId = StatusCatalog.ProcessingId;
        audit.StatementId = null;
        audit.ErrorMessage = null;
        audit.CreatedAtUtc = startedAtUtc;

        await dbContext.ProcessingAudits.AddAsync(audit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return audit;
    }

    public async Task<StatementProcessingAudit> CompleteAsync(
        long processingAuditId,
        long statementId,
        int extractedTransactionCount,
        int ignoredRowCount = 0,
        int correctedRowCount = 0,
        CancellationToken cancellationToken = default)
    {
        if (statementId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statementId),
                "A saved statement identifier is required.");
        }

        ValidateCounts(extractedTransactionCount, ignoredRowCount, correctedRowCount);

        var audit = await GetAuditAsync(processingAuditId, cancellationToken);
        var statementBelongsToUser = await dbContext.Statements
            .AnyAsync(
                statement => statement.Id == statementId &&
                             statement.UserId == audit.UserId,
                cancellationToken);

        if (!statementBelongsToUser)
        {
            throw new InvalidOperationException(
                $"Statement {statementId} does not belong to the processing audit user.");
        }

        var completedAtUtc = DateTime.UtcNow;

        audit.StatementId = statementId;
        audit.CompletedAtUtc = completedAtUtc;
        audit.DurationMilliseconds = GetDurationMilliseconds(
            audit.StartedAtUtc,
            completedAtUtc);
        audit.StatusId = StatusCatalog.SuccessId;
        audit.ExtractedTransactionCount = extractedTransactionCount;
        audit.IgnoredRowCount = ignoredRowCount;
        audit.CorrectedRowCount = correctedRowCount;
        audit.ErrorMessage = null;
        audit.UpdatedAtUtc = completedAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
        return audit;
    }

    public async Task<StatementProcessingAudit> FailAsync(
        long processingAuditId,
        Exception exception,
        int? extractedTransactionCount = null,
        int ignoredRowCount = 0,
        int correctedRowCount = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (extractedTransactionCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extractedTransactionCount));
        }

        ValidateCounts(0, ignoredRowCount, correctedRowCount);

        // A failed statement insert may have left Added entities tracked. Clear them so
        // recording the processing failure does not retry the failed statement graph.
        dbContext.ChangeTracker.Clear();
        var audit = await GetAuditAsync(processingAuditId, cancellationToken);
        var completedAtUtc = DateTime.UtcNow;
        var errorMessage = $"{exception.GetType().FullName}: {exception.Message}";

        audit.CompletedAtUtc = completedAtUtc;
        audit.DurationMilliseconds = GetDurationMilliseconds(
            audit.StartedAtUtc,
            completedAtUtc);
        audit.StatusId = StatusCatalog.FailedId;
        audit.ExtractedTransactionCount = extractedTransactionCount;
        audit.IgnoredRowCount = ignoredRowCount;
        audit.CorrectedRowCount = correctedRowCount;
        audit.ErrorMessage = errorMessage.Length <= MaximumErrorMessageLength
            ? errorMessage
            : errorMessage[..MaximumErrorMessageLength];
        audit.UpdatedAtUtc = completedAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
        return audit;
    }

    private async Task<StatementProcessingAudit> GetAuditAsync(
        long processingAuditId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processingAuditId);

        return await dbContext.ProcessingAudits.SingleOrDefaultAsync(
                   audit => audit.Id == processingAuditId,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   $"Processing audit {processingAuditId} was not found.");
    }

    private static long GetDurationMilliseconds(DateTime startedAtUtc, DateTime completedAtUtc)
    {
        return Math.Max(0, (long)(completedAtUtc - startedAtUtc).TotalMilliseconds);
    }

    private static void ValidateCounts(
        int extractedTransactionCount,
        int ignoredRowCount,
        int correctedRowCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(extractedTransactionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(ignoredRowCount);
        ArgumentOutOfRangeException.ThrowIfNegative(correctedRowCount);
    }
}
