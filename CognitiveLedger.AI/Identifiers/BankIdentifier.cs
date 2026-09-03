using System.Linq;
using System.Text.Json;
using CognitiveLedger.AI.AIPlatform;
using CognitiveLedger.AI.Analyzers;
using CognitiveLedger.AI.Analyzers.Prompts;

namespace CognitiveLedger.AI.Identifiers;

[Flags]
public enum BankIdentificationConfidence
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 4
}

public class IdentifyBankResponse : ResponseBase
{
    public BankName BankName { get; set; }
    public BankIdentificationConfidence Confidence { get; set; }
}

public interface IBankIdentifier
{
    Task<IdentifyBankResponse> IdentifyBankAsync(string statementText);
}

public class BankIdentifier : IBankIdentifier
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private readonly IAiPlatform _aiPlatform;

    // A JSON Schema, rather than the loose "json" format, so Ollama constrains generation to
    // this exact shape instead of letting the model freely invent its own JSON structure.
    private static readonly object ResponseFormat = new
    {
        type = "object",
        properties = new
        {
            bankName = new
            {
                type = new[] { "string", "null" },
                @enum = Enum.GetValues<BankName>()
                    .Where(bank => bank != BankName.Unknown)
                    .Select(bank => bank.ToString())
                    .Cast<string?>()
                    .Append(null)
                    .ToArray()
            },
            confidence = new
            {
                type = "string",
                @enum = new[] { "High", "Medium", "Low" }
            }
        },
        required = new[] { "bankName", "confidence" }
    };

    public BankIdentifier(IAiPlatform aiPlatform)
    {
        _aiPlatform = aiPlatform;
    }

    public async Task<IdentifyBankResponse> IdentifyBankAsync(string statementText)
    {
        var response = new IdentifyBankResponse
        {
            Status = ResponseStatus.Failure
        };

        using var tokenSource = new CancellationTokenSource(_timeout);

        try
        {
            var aiResponse = await _aiPlatform.GenerateAsync(new GenerateRequest
            {
                Model = "qwen3:8b",
                Prompt = $"{BankIdentifierSystemPrompt.SystemMessage}\n\n{statementText}",
                Stream = false,
                Format = ResponseFormat,
                Think = false
            }, tokenSource.Token);

            var parsed = JsonSerializer.Deserialize<BankIdentifierAiResponse>(aiResponse.Response)
                ?? throw new JsonException("AI response deserialized to null.");

            response.BankName = MapBankName(parsed.BankName);
            response.Confidence = MapConfidence(parsed.Confidence);
            response.Status = ResponseStatus.Success;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation timed out");
            response.Status = ResponseStatus.Timeout;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return response;
    }

    private static BankIdentificationConfidence MapConfidence(string? confidence) => confidence switch
    {
        "High" => BankIdentificationConfidence.High,
        "Medium" => BankIdentificationConfidence.Medium,
        "Low" => BankIdentificationConfidence.Low,
        _ => BankIdentificationConfidence.Unknown
    };

    private static BankName MapBankName(string? bankName) => bankName switch
    {
        "SynchronyBank" => BankName.SynchronyBank,
        _ => BankName.Unknown
    };
}
