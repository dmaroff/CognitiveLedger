using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveLedger.Common.Types;
using CognitiveLedger.Data.Models.Base;

// ReSharper disable All
namespace CognitiveLedger.Data.Models.CreditCard;


[Table("CreditCardStatement")]
public class CreditCardStatement : FullModelBase
{
    public long AccountId { get; set; }
    public CreditCardAccount? Account { get; set; }

    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateOnly? ClosingDate { get; set; }
    public DateOnly? PaymentDueDate { get; set; }

    public decimal? PreviousBalance { get; set; }
    public decimal? PaymentsAndCredits { get; set; }
    public decimal? PurchasesTotal { get; set; }
    public decimal? FeesTotal { get; set; }
    public decimal? InterestTotal { get; set; }
    public decimal NewBalance { get; set; }
    public decimal? MinimumPaymentDue { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? AvailableCredit { get; set; }

    public string SourceFileName { get; set; } = string.Empty;
    public string SourceFileHash { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; }
    public ImportStatus ImportStatus { get; set; } = ImportStatus.Processing;
    public string? Notes { get; set; }

    public List<CreditCardTransaction> Transactions { get; set; } = [];
}