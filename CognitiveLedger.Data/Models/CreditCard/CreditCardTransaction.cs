using System;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models.CreditCard;

public sealed class CreditCardTransaction : AuditableModelBase
{
    public long StatementId { get; set; }
    public CreditCardStatement Statement { get; set; } = null!;
    public DateOnly? TransactionDate { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Merchant { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsCredit { get; init; }
    public int Sequence { get; set; }
}
