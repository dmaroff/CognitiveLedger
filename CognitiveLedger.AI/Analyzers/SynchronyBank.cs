using System.Text.Json;
using CognitiveLedger.AI.AIPlatform;
using CognitiveLedger.AI.Analyzers.Prompts;
using CognitiveLedger.Common.Types;

namespace CognitiveLedger.AI.Analyzers;

public enum ResponseStatus
{
    Success,
    Failure,
    Timeout
}

public abstract class ResponseBase
{
    public required ResponseStatus Status { get; set; }
}

public class AnalyzeStatementResponse : ResponseBase
{
    public required CreditCardStatementDto CreditCardStatement { get; set; }
}


public class SynchronyBank
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private readonly IAiPlatform _aiPlatform;

    public SynchronyBank(IAiPlatform aiPlatform)
    {
        _aiPlatform = aiPlatform;
    }
    
    public async Task<AnalyzeStatementResponse> AnalyzeStatement(string statementText)
    {
        var response = new AnalyzeStatementResponse
        {
            Status = ResponseStatus.Failure,
            CreditCardStatement = new CreditCardStatementDto()
        };
        
        using var tokenSource = new CancellationTokenSource(_timeout);

        try
        {
            var aiResponse = await _aiPlatform.GenerateAsync(new GenerateRequest
            {
                Model = "qwen3:8b",
                Prompt = $"{SynchronyBankSystemPrompt.SystemMessage}\n\n{statementText}",
                Stream = false,
                Format = "json"
            }, tokenSource.Token);

            var parsed = JsonSerializer.Deserialize<SynchronyBankAiResponse>(aiResponse.Response)
                ?? throw new JsonException("AI response deserialized to null.");

            response.CreditCardStatement = MapToStatementDto(parsed);
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

    private static CreditCardStatementDto MapToStatementDto(SynchronyBankAiResponse parsed)
    {
        return new CreditCardStatementDto
        {
            IssuerName = "Synchrony Bank",
            LastFourDigits = parsed.AccountLastFourDigits ?? string.Empty,
            PeriodStartDate = parsed.PeriodStartDate ?? default,
            PeriodEndDate = parsed.PeriodEndDate ?? default,
            PaymentDueDate = parsed.TotalPaymentDue?.DueDate,
            MinimumPaymentDue = parsed.MinimumPaymentDue?.Amount,
            NewBalance = parsed.TotalPaymentDue?.Amount ?? 0m,
            Transactions = parsed.Transactions
                .Select((transaction, index) => new CreditCardTransactionDto
                {
                    TransactionDate = transaction.TransactionDate,
                    RawDescription = transaction.Description,
                    Amount = transaction.Amount,
                    ReferenceNumber = transaction.ReferenceNumber,
                    TransactionType = MapTransactionType(transaction.Type),
                    SourceSequence = index
                })
                .ToList()
        };
    }

    private static TransactionType MapTransactionType(string type) => type switch
    {
        "Purchase" => TransactionType.Purchase,
        "Return" => TransactionType.Refund,
        _ => TransactionType.Unknown
    };
}