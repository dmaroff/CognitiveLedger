using CognitiveLedger.Common.Types;

namespace CognitiveLedger.AI;

public sealed class CreditCardStatementDto
{
    public string IssuerName { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string LastFourDigits { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    
    public long AccountId { get; set; }

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

    public List<CreditCardTransactionDto> Transactions { get; set; } = [];
}

public class CreditCardTransactionDto
{
    public long StatementId { get; set; }

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
    public string? UserNotes { get; set; }
}