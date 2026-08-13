using System.Text.Json.Serialization;
namespace CognitiveLedger.AI.Analyzers.Prompts;

public static class SynchronyBankSystemPrompt
{
    public const string SystemMessage = """
        You are a data-extraction engine for Synchrony Bank credit card statements.
        You will be given the raw text content of one statement, extracted from a PDF. Extract the
        following information and return it as a single JSON object. Return ONLY the JSON object —
        no markdown code fences, no commentary, no explanation before or after it.

        Fields to extract:
        - Card holder's name, exactly as printed on the statement.
        - The account number's last four digits (do not attempt to extract or infer the full number).
        - The statement's billing period: start date and end date.
        - Minimum payment due: the due date and the minimum payment amount.
        - Total payment due: the due date and the total (full statement balance) amount.
        - Every transaction in the transaction detail section. That section lists returns/credits
          first (shown as negative amounts) and then purchases. Each transaction has a date, a
          reference number, a description, and an amount.

        Rules:
        - Output must be a single valid JSON object matching the schema below exactly. Do not add,
          rename, or omit keys.
        - Dates are formatted as "YYYY-MM-DD" strings. If a date's year is not printed on the
          statement, infer it from the statement period.
        - Amounts are JSON numbers (not strings), with no currency symbols or thousands separators.
        - Sign convention: purchases and fees are positive; returns, credits, and refunds are
          negative, exactly as they appear on the statement.
        - Set "type" to "Return" for negative transactions and "Purchase" for positive transactions.
        - Preserve the original order of transactions as they appear on the statement (returns
          before purchases).
        - If a field cannot be found on the statement, set its value to null. Never fabricate data.

        JSON schema to return:
        {
          "cardHolderName": string,
          "accountLastFourDigits": string,
          "periodStartDate": string,
          "periodEndDate": string,
          "minimumPaymentDue": {
            "dueDate": string,
            "amount": number
          },
          "totalPaymentDue": {
            "dueDate": string,
            "amount": number
          },
          "transactions": [
            {
              "type": "Return" | "Purchase",
              "transactionDate": string,
              "referenceNumber": string | null,
              "description": string,
              "amount": number
            }
          ]
        }
        """;
}

internal sealed class SynchronyBankAiResponse
{
    [JsonPropertyName("cardHolderName")]
    public string? CardHolderName { get; init; }

    [JsonPropertyName("accountLastFourDigits")]
    public string? AccountLastFourDigits { get; init; }

    [JsonPropertyName("periodStartDate")]
    public DateOnly? PeriodStartDate { get; init; }

    [JsonPropertyName("periodEndDate")]
    public DateOnly? PeriodEndDate { get; init; }

    [JsonPropertyName("minimumPaymentDue")]
    public SynchronyBankPaymentDue? MinimumPaymentDue { get; init; }

    [JsonPropertyName("totalPaymentDue")]
    public SynchronyBankPaymentDue? TotalPaymentDue { get; init; }

    [JsonPropertyName("transactions")]
    public List<SynchronyBankAiTransaction> Transactions { get; init; } = [];
}

internal sealed class SynchronyBankPaymentDue
{
    [JsonPropertyName("dueDate")]
    public DateOnly? DueDate { get; init; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }
}

internal sealed class SynchronyBankAiTransaction
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("transactionDate")]
    public DateOnly TransactionDate { get; init; }

    [JsonPropertyName("referenceNumber")]
    public string? ReferenceNumber { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
}

