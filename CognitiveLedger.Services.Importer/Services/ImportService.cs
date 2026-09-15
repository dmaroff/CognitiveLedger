using System.Security.Cryptography;
using CognitiveLedger.AI.OpenAI;
using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.Common;
using CognitiveLedger.Common.Response;
using CognitiveLedger.Data.Repositories;
using CognitiveLedger.Data.Models.CreditCard;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;


namespace CognitiveLedger.Services.Importer.Services;

public sealed class ImportService : IImportService
{
    private readonly IPdfTextExtractor _extractor;
    private readonly IStatementRepository _statementRepository;
    private readonly IStatementProcessingRepository _processingRepository;
    private readonly IPiiSanitizer _piiSanitizer;
    private readonly IOpenAiPdfStatementReader _statementReader;
    private readonly IAppConfiguration _config;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        IPdfTextExtractor extractor,
        IPiiSanitizer piiSanitizer,
        IOpenAiPdfStatementReader statementReader,
        IStatementRepository statementRepository,
        IStatementProcessingRepository processingRepository,
        IAppConfiguration config,
        ILogger<ImportService> logger)
    {
        _extractor = extractor;
        _piiSanitizer = piiSanitizer;
        _statementReader = statementReader;
        _statementRepository = statementRepository;
        _processingRepository = processingRepository;
        _config = config;
        _logger = logger;
    }
    
    public async Task<ImportResponse> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        cancellationToken.ThrowIfCancellationRequested();

        var jobId = Guid.NewGuid();
        var sourceDocumentSha256 = Convert.ToHexString(SHA256.HashData(request.FileData));

        _logger.LogInformation(
            "Import job {JobId} received a {FileType} document",
            jobId,
            request.FileType);

        return request.FileType switch
        {
            ImportFileType.Pdf => await ImportPdfAsync(
                request, jobId, sourceDocumentSha256, cancellationToken),
            ImportFileType.Csv or ImportFileType.Jpeg => NotConfigured(
                jobId,
                "FILE_TYPE_NOT_CONFIGURED",
                $"{request.FileType} imports are not configured yet."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(request.FileType), request.FileType, "Unsupported import file type.")
        };
    }

    private async Task<ImportResponse> ImportPdfAsync(
        ImportRequest request,
        Guid jobId,
        string sourceDocumentSha256,
        CancellationToken cancellationToken)
    {
        if (request.StatementType != StatementType.CreditCard)
        {
            return NotConfigured(
                jobId,
                "STATEMENT_TYPE_NOT_CONFIGURED",
                $"{request.StatementType} PDF imports are not configured yet.");
        }
        
        var requestTimeoutSeconds = _config.AiRequestTimeoutSeconds;
        if (requestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "OpenAI:RequestTimeoutSeconds must be a positive integer.");
        }

        // Step 1: Check whether this source document has already been imported.
        var existingStatement = await _statementRepository
            .FindBySourceDocumentSha256Async(sourceDocumentSha256, cancellationToken);

        if (existingStatement is not null)
        {
            return Existing(existingStatement.Id);
        }
        
        var audit = await StartProcessingAuditAsync(
            request.StatementType.ToString(),
            _config.AiProvider,
            _config.AiModel,
            cancellationToken);
        
        var sanitizePiiResponse = await SanitizePdfAsync(request, cancellationToken);
        
        if (sanitizePiiResponse.PiiStatus == PiiDetectionStatus.DetectedAndRemoved)
        {
            _logger.LogInformation(
                "Import job {JobId} detected and removed {PiiItemCount} PII items from the PDF",
                jobId,
                sanitizePiiResponse.PiiItems.Count);
        }
        else
        {
            _logger.LogInformation(
                "Import job {JobId} did not detect any PII items in the PDF",
                jobId);
        }

        var statementResponse = await ReadStatementAsync(requestTimeoutSeconds, sanitizePiiResponse);
        if (statementResponse.Status != ResponseStatus.Success)
        {
            return Failed(statementResponse);
        }
        
        var savedStatement = await PersistStatementAsync(
                MapToCreditCardStatement(statementResponse, sourceDocumentSha256),
                cancellationToken);
        
        await CompleteProcessingAuditAsync(
            audit.Id,
            savedStatement.Id,
            savedStatement.Transactions.Count,
            cancellationToken);
        
        return Success(savedStatement.Id);
    }

    private async Task<ExtractPdfStatementResponse> ReadStatementAsync(
        int requestTimeoutSeconds,
        SanitizePiiResponse sanitizePiiResponse)
    {
        try
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(requestTimeoutSeconds));
            var extractedStatement = await _statementReader.ExtractAsync(
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
                "OpenAI PDF statement extraction timed out after {TimeoutSeconds} seconds",
                requestTimeoutSeconds);
            return new ExtractPdfStatementResponse
            {
                Status = ResponseStatus.Failed,
                ErrorCode = "OPENAI_TIMEOUT",
                ErrorMessage = $"OpenAI PDF statement extraction timed out after {requestTimeoutSeconds} seconds.",
                Statement = null
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // These stage adapters are ready for the source-specific PDF parser workflow.
    // They are not invoked until extraction and validation are connected.
    private Task<SanitizePiiResponse> SanitizePdfAsync(
        ImportRequest request,
        CancellationToken cancellationToken) =>
        _piiSanitizer.SanitizePiiAsync(new SanitizePiiRequest
        {
            PdfData = request.FileData,
            PiiValues = request.PiiToRedact
        }, cancellationToken);

    private Task<StatementProcessingAudit> StartProcessingAuditAsync(
        string parserName,
        string aiProvider,
        string aiModel,
        CancellationToken cancellationToken) =>
        _processingRepository.StartAsync(new StatementProcessingAudit
        {
            StatementType = parserName,
            AiProvider = aiProvider,
            AiModel = aiModel
        }, cancellationToken);

    private Task<CreditCardStatement> PersistStatementAsync(
        CreditCardStatement statement,
        CancellationToken cancellationToken) =>
        _statementRepository.InsertStatementAsync(statement, cancellationToken);

    private Task<StatementProcessingAudit> CompleteProcessingAuditAsync(
        long auditId,
        long statementId,
        int transactionCount,
        CancellationToken cancellationToken) =>
        _processingRepository.CompleteAsync(
            auditId, statementId, transactionCount,
            cancellationToken: cancellationToken);

    private Task<StatementProcessingAudit> FailProcessingAuditAsync(
        long auditId,
        Exception exception,
        CancellationToken cancellationToken) =>
        _processingRepository.FailAsync(
            auditId, exception,
            cancellationToken: cancellationToken);

    private static ImportResponse NotConfigured(
        Guid jobId,
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
    
    private static ImportResponse Existing(long statementId) => new()
    {
        Status = ResponseStatus.Success,
        StatementId = statementId
    };
    
    private static ImportResponse Success(long statementId) => new()
    {
        Status = ResponseStatus.Success,
        StatementId = statementId
    };

    private static void ValidateRequest(ImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FileType is ImportFileType.Unknown ||
            !Enum.IsDefined(request.FileType))
        {
            throw new ArgumentException("A supported file type is required.", nameof(request));
        }

        if (request.FileData.Length == 0)
        {
            throw new ArgumentException("Non-empty file data is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("A file name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SourceName))
        {
            throw new ArgumentException("A source name is required.", nameof(request));
        }

        if (request.StatementType is StatementType.Unknown ||
            !Enum.IsDefined(request.StatementType))
        {
            throw new ArgumentException("A supported statement type is required.", nameof(request));
        }
    }
    
    private static CreditCardStatement MapToCreditCardStatement(
        ExtractPdfStatementResponse result,
        string sourceDocumentSha256)
    {
        ArgumentNullException.ThrowIfNull(result);
        var statement = result.Statement!;

        return new CreditCardStatement
        {
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
