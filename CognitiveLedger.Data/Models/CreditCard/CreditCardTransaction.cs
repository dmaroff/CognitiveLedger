// ReSharper disable All
using System;
using System.ComponentModel.DataAnnotations.Schema;
using CognitiveLedger.Common.Types;
using CognitiveLedger.Data.Models.Base;

namespace CognitiveLedger.Data.Models.CreditCard;

[Table("CreditCardTransaction")]
public class CreditCardTransaction : FullModelBase
{
    public long StatementId { get; set; }
    public CreditCardStatement? Statement { get; set; }

    public DateOnly TransactionDate { get; set; }
    public DateOnly? PostedDate { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public TransactionType TransactionType { get; set; } = TransactionType.Unknown;
    public string? Category { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? OriginalCurrencyCode { get; set; }
    public int? SourcePageNumber { get; set; }
    public int SourceSequence { get; set; }
    public decimal? AiConfidence { get; set; }
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Unreviewed;
    public string? UserNotes { get; set; }
}
