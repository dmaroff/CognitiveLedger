using System.Security.Cryptography;
using CognitiveLedger.Common;
using CognitiveLedger.Common.Response;
using CognitiveLedger.Data.Repositories;
using CognitiveLedger.Data.Models.CreditCard;
using CognitiveLedger.Common.Types;
using AiModelCatalog = CognitiveLedger.Data.Models.AiModelCatalog;
using AiProviderCatalog = CognitiveLedger.Data.Models.AiProviderCatalog;
using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;
using CognitiveLedger.Statements.Abstractions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;


namespace CognitiveLedger.Services.Importer.Services;

public sealed class ImportService : IImportService
{
    private readonly IStatementRepository _statementRepository;
    private readonly IStatementProcessingRepository _processingRepository;
    private readonly IStatementDefinitionResolver _definitionResolver;
    private readonly IStatementDefinitionDetector _definitionDetector;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IAppConfiguration _config;
    private readonly ILocalQueueService _localQueueService;
    private readonly IAppLog<ImportService> _logger;

    public ImportService(
        IStatementRepository statementRepository,
        IStatementProcessingRepository processingRepository,
        IStatementDefinitionResolver definitionResolver,
        IStatementDefinitionDetector definitionDetector,
        IPdfTextExtractor pdfTextExtractor,
        IAppConfiguration config,
        ILocalQueueService localQueueService,
        IAppLog<ImportService> logger)
    {
        _statementRepository = statementRepository;
        _processingRepository = processingRepository;
        _definitionResolver = definitionResolver;
        _definitionDetector = definitionDetector;
        _pdfTextExtractor = pdfTextExtractor;
        _config = config;
        _localQueueService = localQueueService;
        _logger = logger;
    }
    
    public async Task<ImportResponse> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogMethodStart(request);
        ValidateRequest(request);
        cancellationToken.ThrowIfCancellationRequested();
        
        var sourceDocumentSha256 = Convert.ToHexString(SHA256.HashData(request.FileData));

        return request.FileType switch
        {
            ImportFileType.Pdf => await ImportPdfAsync(
                request, sourceDocumentSha256, cancellationToken),
            
            ImportFileType.Csv or ImportFileType.Jpeg => NotConfigured(
                "FILE_TYPE_NOT_CONFIGURED",
                $"{request.FileType} imports are not configured yet."),
            
            _ => throw new ArgumentOutOfRangeException(
                nameof(request), request.FileType, "Unsupported import file type.")
        };
    }

    private async Task<ImportResponse> ImportPdfAsync(
        ImportRequest request,
        string sourceDocumentSha256,
        CancellationToken cancellationToken)
    {
        if (request.StatementType != StatementType.CreditCard)
        {
            return NotConfigured(
                "STATEMENT_TYPE_NOT_CONFIGURED",
                $"{request.StatementType} PDF imports are not configured yet.");
        }

        var definition = ResolveDefinition(request);

        if (definition is null)
        {
            var hasExplicitDefinition = !string.IsNullOrWhiteSpace(
                request.StatementDefinitionKey);
            return NotConfigured(
                hasExplicitDefinition
                    ? "STATEMENT_DEFINITION_NOT_CONFIGURED"
                    : "STATEMENT_DEFINITION_NOT_IDENTIFIED",
                hasExplicitDefinition
                    ? $"Statement definition '{request.StatementDefinitionKey}' is not configured."
                    : "The statement could not be identified from its PDF content. " +
                      "Provide a supported statement definition key.");
        }
        
        var requestTimeoutSeconds = _config.AiRequestTimeoutSeconds;
        if (requestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "OpenAI:RequestTimeoutSeconds must be a positive integer.");
        }

        // Step 1: Check whether this source document has already been imported.
        var existingStatement = await _statementRepository
            .FindBySourceDocumentSha256Async(
                request.UserId,
                sourceDocumentSha256,
                cancellationToken);

        if (existingStatement is not null)
        {
            return Existing(existingStatement.Id);
        }
        
        var audit = await StartProcessingAuditAsync(
            request.UserId,
            request.StatementType,
            request.FileName,
            cancellationToken);
        
        var identifiedRequest = request.WithStatementDefinitionKey(
            definition.Descriptor.Key);
        _localQueueService.Enqueue(identifiedRequest, audit.Id);
        return Queued(audit.Id);
    }

    private IStatementDefinition? ResolveDefinition(ImportRequest request)
    {
        var statementKind = request.StatementType.ToStatementKind();
        if (!string.IsNullOrWhiteSpace(request.StatementDefinitionKey))
        {
            return _definitionResolver.Resolve(new StatementDefinitionSelector(
                request.StatementDefinitionKey,
                request.SourceName,
                statementKind));
        }

        var extractedText = _pdfTextExtractor.ExtractPdfText(new ExtractPdfTextRequest
        {
            UserId = request.UserId,
            PdfData = request.FileData
        });

        var detected = _definitionDetector.Detect(
            extractedText.FullText,
            statementKind);

        return detected ?? _definitionResolver.Resolve(new StatementDefinitionSelector(
            null,
            request.SourceName,
            statementKind));
    }

    private Task<StatementProcessingAudit> StartProcessingAuditAsync(
        long userId,
        StatementType statementType,
        string filename,
        CancellationToken cancellationToken) =>
        _processingRepository.StartAsync(new StatementProcessingAudit
        {
            UserId = userId,
            Filename = filename,
            StatementTypeId = statementType switch
            {
                StatementType.CreditCard => StatementTypeCatalog.CreditCardId,
                _ => throw new ArgumentOutOfRangeException(nameof(statementType), statementType, "Unsupported statement type.")
            },
            AiProviderId = GetAiProviderId(_config.AiProvider),
            AiModelId = GetAiModelId(_config.AiProvider, _config.AiModel)
        }, cancellationToken);

    private static long GetAiProviderId(string provider) => provider switch
    {
        "OpenAI" => AiProviderCatalog.OpenAiId,
        _ => throw new InvalidOperationException($"AI provider '{provider}' is not configured in the database catalog.")
    };

    private static long GetAiModelId(string provider, string model) => (provider, model) switch
    {
        ("OpenAI", "gpt-5.6-terra") => AiModelCatalog.Gpt56TerraId,
        _ => throw new InvalidOperationException($"AI model '{provider}/{model}' is not configured in the database catalog.")
    };

    private static ImportResponse NotConfigured(
        string errorCode,
        string errorMessage) => new()
    {
        Status = ResponseStatus.Failed,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
    
    private static ImportResponse Existing(long statementId) => new()
    {
        Status = ResponseStatus.Success,
        Existing = true,
        StatementId = statementId
    };
    
    private static ImportResponse Queued(long processingAuditId) => new()
    {
        Status = ResponseStatus.Success,
        ProcessingAuditId = processingAuditId
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

        if (request.StatementType is StatementType.Unknown ||
            !Enum.IsDefined(request.StatementType))
        {
            throw new ArgumentException("A supported statement type is required.", nameof(request));
        }
    }
}
