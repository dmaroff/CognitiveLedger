using System.Text.Json.Serialization;

namespace CognitiveLedger.AI.OpenAI.Response;

internal sealed class ExtractPageTransactionsResponse
{
    [JsonPropertyName("transactions")]
    public required IReadOnlyList<ExtractedPageTransaction> Transactions { get; init; }
}

internal sealed class ExtractedPageTransaction
{
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
