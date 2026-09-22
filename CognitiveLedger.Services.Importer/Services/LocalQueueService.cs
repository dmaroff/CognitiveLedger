using System.Collections.Concurrent;
using System.Security.Cryptography;
using CognitiveLedger.AI.OpenAI;
using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.Common;
using CognitiveLedger.Common.Response;
using CognitiveLedger.Data.Models.CreditCard;
using CognitiveLedger.Data.Repositories;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using CognitiveLedger.Services.Importer.Exceptions;
using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Services.Importer.Services;

public sealed class LocalQueueService : ILocalQueueService
{
    private readonly AppLog<LocalQueueService> _logger;
    private readonly IPiiSanitizer _piiSanitizer;
    private readonly IServiceScopeFactory _scopeFactory;
    
    private readonly ConcurrentQueue<QueueItem> _queue = new ConcurrentQueue<QueueItem>();

    public LocalQueueService(
        IPiiSanitizer piiSanitizer,
        IServiceScopeFactory scopeFactory,
        AppLog<LocalQueueService> logger)
    {
        _piiSanitizer = piiSanitizer;
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Start the queue processing task
        Task.Run(() => RunQueueProcessor(CancellationToken.None));
    }
    
    public void Enqueue(ImportRequest request, long auditId)
    {
        _logger.LogMethodStart();
        _queue.Enqueue(new QueueItem { AuditId = auditId, Request = request });
        _logger.LogInfo($"Enqueued: ({request.FileName})");
        _logger.LogMethodEnd();
    }
    
    private bool TryDequeue(out QueueItem? request)
    {
        if (!_queue.TryDequeue(out var queueItem))
        {
            request = null;
            return false;
        }
        request = queueItem;
        _logger.LogInfo($"Dequeued: ({request.Request.FileName})");
        return true;
    }
    
    private async Task RunQueueProcessor(CancellationToken cancellationToken)
    {
        _logger.LogMethodStart();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            if (TryDequeue(out var queueItem))
            {
                // Process the request here
                await ProcessRequestAsync(queueItem!.AuditId, queueItem!.Request, cancellationToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
        }
        _logger.LogMethodEnd();
    }
    
    private static void ValidateConfiguration(IAppConfiguration config)
    {
        if (config.AiRequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "OpenAI:RequestTimeoutSeconds must be a positive integer.");
        }
    }

    private async Task<ImportResponse> ProcessRequestAsync(
        long auditId,
        ImportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogMethodStart();
        _logger.LogInfo($"Processing: ({request})");

        using var scope = _scopeFactory.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IAppConfiguration>();
        var statementReader = scope.ServiceProvider.GetRequiredService<IOpenAiPdfStatementReader>();
        var statementRepository = scope.ServiceProvider.GetRequiredService<IStatementRepository>();
        var processingRepository = scope.ServiceProvider
            .GetRequiredService<IStatementProcessingRepository>();

        ValidateConfiguration(config);
        var sourceDocumentSha256 = Convert.ToHexString(SHA256.HashData(request.FileData));
        
        if (request.StatementType != StatementType.CreditCard)
        {
            return NotConfigured(
                "STATEMENT_TYPE_NOT_CONFIGURED",
                $"{request.StatementType} PDF imports are not configured yet.");
        }
        
        
        var sanitizePiiResponse = await SanitizePdfAsync(request, cancellationToken);

        _logger.LogInfo(request,
            sanitizePiiResponse.PiiStatus == PiiDetectionStatus.DetectedAndRemoved
                ? $"Import job removed {sanitizePiiResponse.PiiItems.Count} PII items from the PDF"
                : $"Import job did not detect any PII items in the PDF");

        var statementResponse = await ReadStatementAsync(
            request,
            sanitizePiiResponse,
            config,
            statementReader);
        if (statementResponse.Status != ResponseStatus.Success)
        {
            return Failed(statementResponse);
        }
        
        var savedStatement = await PersistStatementAsync(
            statementRepository,
            MapToCreditCardStatement(
                request.UserId,
                statementResponse,
                sourceDocumentSha256),
            cancellationToken);
        
        await CompleteProcessingAuditAsync(
            processingRepository,
            auditId,
            savedStatement.Id,
            savedStatement.Transactions.Count,
            cancellationToken);
        
        _logger.LogMethodEnd();
        return Success(savedStatement.Id);
    }
    
    private async Task<ExtractPdfStatementResponse> ReadStatementAsync(
        ImportRequest request,
        SanitizePiiResponse sanitizePiiResponse,
        IAppConfiguration config,
        IOpenAiPdfStatementReader statementReader)
    {
        try
        {
            _logger.LogMethodStart(request);
            using var tokenSource = new CancellationTokenSource(
                TimeSpan.FromSeconds(config.AiRequestTimeoutSeconds));
            var extractedStatement = await statementReader.ExtractAsync(
                new SynchronyAmazonStatementRequest
                {
                    PdfData = sanitizePiiResponse.RasterizedPdfData
                },
                tokenSource.Token);
            return extractedStatement;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                $"OpenAI PDF statement extraction timed out after {config.AiRequestTimeoutSeconds} seconds");
            return new ExtractPdfStatementResponse
            {
                Status = ResponseStatus.Failed,
                ErrorCode = "OPENAI_TIMEOUT",
                ErrorMessage = $"OpenAI PDF statement extraction timed out after {config.AiRequestTimeoutSeconds} seconds.",
                Statement = null
            };
        }
        catch (Exception e)
        {
            _logger.LogError(request, e);
            throw;
        }
        finally
        {
            _logger.LogMethodEnd(request);
        }
    }
    
    // These stage adapters are ready for the source-specific PDF parser workflow.
    // They are not invoked until extraction and validation are connected.
    private Task<SanitizePiiResponse> SanitizePdfAsync(
        ImportRequest request,
        CancellationToken cancellationToken) =>
        _piiSanitizer.SanitizePiiAsync(new SanitizePiiRequest
        {
            UserId = request.UserId,
            PdfData = request.FileData,
            PiiValues = request.PiiToRedact
        }, cancellationToken);

    private Task<CreditCardStatement> PersistStatementAsync(
        IStatementRepository statementRepository,
        CreditCardStatement statement,
        CancellationToken cancellationToken) =>
        statementRepository.InsertStatementAsync(statement, cancellationToken);

    private Task<StatementProcessingAudit> CompleteProcessingAuditAsync(
        IStatementProcessingRepository processingRepository,
        long auditId,
        long statementId,
        int transactionCount,
        CancellationToken cancellationToken) =>
        processingRepository.CompleteAsync(
            auditId, statementId, transactionCount,
            cancellationToken: cancellationToken);

    private static ImportResponse NotConfigured(
        string errorCode,
        string errorMessage) => new()
    {
        Status = ResponseStatus.Failed,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
    
    private static ImportResponse Failed(ExtractPdfStatementResponse response) => new()
    {
        Status = ResponseStatus.Failed,
        ErrorCode = response.ErrorCode,
        ErrorMessage = response.ErrorMessage
    };
    
    private static ImportResponse Success(long statementId) => new()
    {
        Status = ResponseStatus.Success,
        StatementId = statementId
    };
    
    private static CreditCardStatement MapToCreditCardStatement(
        long userId,
        ExtractPdfStatementResponse result,
        string sourceDocumentSha256)
    {
        ArgumentNullException.ThrowIfNull(result);
        var statement = result.Statement!;

        return new CreditCardStatement
        {
            UserId = userId,
            SourceDocumentSha256 = sourceDocumentSha256,
            Issuer = statement.Issuer,
            AccountName = statement.AccountName,
            StatementPeriodStart = statement.StatementPeriodStart,
            StatementPeriodEnd = statement.StatementPeriodEnd,
            PreviousBalance = statement.PreviousBalance,
            NewBalance = statement.NewBalance,
            TotalPurchases = statement.TotalPurchases,
            TotalPayments = statement.TotalPayments,
            TotalOtherCredits = statement.TotalOtherCredits,
            Fees = statement.Fees,
            InterestCharged = statement.InterestCharged,
            Transactions = [ .. statement.Transactions
                .Select(transaction => new CreditCardTransaction
                {
                    TransactionDate = transaction.Date,
                    Category = transaction.Category,
                    Merchant = transaction.Merchant,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    IsCredit = transaction.IsCredit
                })]
        };
    }
}

internal class QueueItem
{
    public required long AuditId { get; init; }
    public required ImportRequest Request { get; init; }
}
