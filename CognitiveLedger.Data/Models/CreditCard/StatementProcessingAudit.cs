using System;
using System.ComponentModel.DataAnnotations;
using CognitiveLedger.Data.Models;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models.CreditCard;

public sealed class StatementProcessingAudit : AuditableModelBase
{
    public long? StatementId { get; set; }
    public CreditCardStatement? Statement { get; init; }
    [MaxLength(255)]
    public string? Filename { get; init; }
    public long StatementTypeId { get; init; }
    public StatementType? StatementType { get; init; }
    public long AiProviderId { get; init; }
    public AiProvider AiProvider { get; init; } = null!;
    public long AiModelId { get; init; }
    public AiModel AiModel { get; init; } = null!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMilliseconds { get; set; }
    public long StatusId { get; set; }
    public Status Status { get; init; } = null!;
    public int? PageCount { get; init; }
    public int? ExtractedTransactionCount { get; set; }
    public int IgnoredRowCount { get; set; }
    public int CorrectedRowCount { get; set; }
    public string? ErrorMessage { get; set; }
}
