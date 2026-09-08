using System.Text.Json.Serialization;

namespace CognitiveLedger.AI.OpenAI.Response;

internal sealed class ExtractStatementSummaryResponse
{
    [JsonPropertyName("statement_summary")]
    public required ExtractedStatementSummary StatementSummary { get; init; }
}

internal sealed class ExtractedStatementSummary
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

    [JsonPropertyName("total_payments")]
    public required decimal TotalPayments { get; init; }

    [JsonPropertyName("total_other_credits")]
    public required decimal TotalOtherCredits { get; init; }

    [JsonPropertyName("fees")]
    public required decimal Fees { get; init; }

    [JsonPropertyName("interest_charged")]
    public required decimal InterestCharged { get; init; }
}
