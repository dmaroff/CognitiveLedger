using System;
using System.Collections.Generic;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models.CreditCard;

public sealed class CreditCardStatement : AuditableModelBase
{
    public string? SourceDocumentSha256 { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public DateOnly StatementPeriodStart { get; init; }
    public DateOnly StatementPeriodEnd { get; init; }
    public decimal PreviousBalance { get; init; }
    public decimal NewBalance { get; init; }
    public decimal TotalPurchases { get; init; }
    public decimal TotalPayments { get; init; }
    public decimal TotalOtherCredits { get; init; }
    public decimal Fees { get; init; }
    public decimal InterestCharged { get; init; }

    public ICollection<CreditCardTransaction> Transactions { get; init; } = [];
    public ICollection<StatementProcessingAudit> ProcessingAudits { get; init; } = [];
}
