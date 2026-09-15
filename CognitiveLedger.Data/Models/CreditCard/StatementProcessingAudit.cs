using System;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models.CreditCard;

public sealed class StatementProcessingAudit : AuditableModelBase
{
    public long? StatementId { get; set; }
    public CreditCardStatement? Statement { get; set; }
    public string StatementType { get; init; } = string.Empty;
    public string AiProvider { get; init; } = string.Empty;
    public string AiModel { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMilliseconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PageCount { get; init; }
    public int? ExtractedTransactionCount { get; set; }
    public int IgnoredRowCount { get; set; }
    public int CorrectedRowCount { get; set; }
    public string? ErrorMessage { get; set; }
}
