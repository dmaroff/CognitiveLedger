using System.Text.Json.Serialization;

namespace CognitiveLedger.AI.OpenAI.Response;

public sealed class ExtractPdfStatementResponse
{
    [JsonPropertyName("statement")]
    public required ExtractedStatement Statement { get; init; }
}

public sealed class ExtractedStatement
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    [JsonPropertyName("account_name")]
    public required string AccountName { get; init; }

    [JsonPropertyName("statement_period_start")]
    public required DateOnly StatementPeriodStart { get; init; }

    [JsonPropertyName("statement_period_end")]
    public required DateOnly StatementPeriodEnd { get; init; }

    [JsonPropertyName("previous_balance")]
    public required decimal PreviousBalance { get; init; }

    [JsonPropertyName("new_balance")]
    public required decimal NewBalance { get; init; }

    [JsonPropertyName("total_purchases")]
    public required decimal TotalPurchases { get; init; }

    [JsonPropertyName("total_payments")]
    public required decimal TotalPayments { get; init; }

    [JsonPropertyName("total_other_credits")]
    public required decimal TotalOtherCredits { get; init; }

    [JsonPropertyName("fees")]
    public required decimal Fees { get; init; }

    [JsonPropertyName("interest_charged")]
    public required decimal InterestCharged { get; init; }

    [JsonPropertyName("transactions")]
    public required IReadOnlyList<ExtractedTransaction> Transactions { get; init; }

    [JsonIgnore]
    public decimal NetNewSpending => TotalPurchases - TotalOtherCredits;
}

public sealed class ExtractedTransaction
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("date")]
    public DateOnly? Date { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("merchant")]
    public required string Merchant { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; init; }

    [JsonPropertyName("is_credit")]
    public required bool IsCredit { get; init; }
}
