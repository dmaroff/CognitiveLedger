using System;
using System.Threading;
using System.Threading.Tasks;
using CognitiveLedger.Data.Models.CreditCard;

namespace CognitiveLedger.Data.Repositories;

public interface IStatementProcessingRepository
{
    Task<StatementProcessingAudit> StartAsync(
        StatementProcessingAudit audit,
        CancellationToken cancellationToken = default);

    Task<StatementProcessingAudit> CompleteAsync(
        long processingAuditId,
        long statementId,
        int extractedTransactionCount,
        int ignoredRowCount = 0,
        int correctedRowCount = 0,
        CancellationToken cancellationToken = default);

    Task<StatementProcessingAudit> FailAsync(
        long processingAuditId,
        Exception exception,
        int? extractedTransactionCount = null,
        int ignoredRowCount = 0,
        int correctedRowCount = 0,
        CancellationToken cancellationToken = default);
}
