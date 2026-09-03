using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CognitiveLedger.AI.Identifiers;

namespace CognitiveLedger.AI.Analyzers.Prompts;

public static class BankIdentifierSystemPrompt
{
    public static string SystemMessage { get; } = BuildSystemMessage();

    private static string BuildSystemMessage()
    {
        var bankNames = Enum.GetValues<BankName>()
            .Where(bank => bank != BankName.Unknown)
            .ToList();

        var bankRules = string.Join(
            "\n",
            bankNames.Select(bank => $"  - \"{bank}\" — {ToDisplayName(bank)}"));

        var allowedValues = string.Join(" | ", bankNames.Select(bank => $"\"{bank}\""));

        return $$"""
            You are a classification engine for bank and credit card statements.
            You will be given the raw text content of one statement, extracted from a PDF. Your only
            job is to identify which bank or card issuer produced the statement. Return ONLY a single
            JSON object — no markdown code fences, no commentary, no explanation before or after it.

            Rules:
            - Set "bankName" to exactly one of the following values, based on which bank or card
              issuer produced the statement:
            {{bankRules}}
            - Base your answer only on what is printed in the statement text (letterhead, logo text,
              remittance address, customer service references, legal footer, etc.). Never fabricate
              or guess a bank that isn't evidenced by the text.
            - If the statement text does not clearly identify one of the banks above, set "bankName"
              to null.
            - "confidence" should be "High", "Medium", or "Low" depending on how clearly the text
              identifies the issuer.

            JSON schema to return:
            {
              "bankName": {{allowedValues}} | null,
              "confidence": "High" | "Medium" | "Low"
            }
            """;
    }

    private static string ToDisplayName(BankName bank) =>
        Regex.Replace(bank.ToString(), "(?<!^)([A-Z])", " $1");
}

internal sealed class BankIdentifierAiResponse
{
    [JsonPropertyName("bankName")]
    public string? BankName { get; init; }

    [JsonPropertyName("confidence")]
    public string? Confidence { get; init; }
}
