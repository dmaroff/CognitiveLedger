using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Exceptions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.Parser.PDF;

/// <summary>
/// Coordinates the complete PDF PII sanitization process:
/// extracts text, detects PII, redacts it, and verifies its removal.
/// </summary>
public sealed class PdfPiiSanitizer : IPiiSanitizer
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IPiiDetector _piiDetector;
    private readonly IPdfRedactor _pdfRedactor;
    private readonly IPdfRasterizer _pdfRasterizer;
    private readonly ILogger<PdfPiiSanitizer> _logger;

    public PdfPiiSanitizer(
        IPdfTextExtractor pdfTextExtractor,
        IPiiDetector piiDetector,
        IPdfRedactor pdfRedactor,
        IPdfRasterizer pdfRasterizer,
        ILogger<PdfPiiSanitizer>? logger = null)
    {
        _pdfTextExtractor = pdfTextExtractor
            ?? throw new ArgumentNullException(nameof(pdfTextExtractor));

        _piiDetector = piiDetector
            ?? throw new ArgumentNullException(nameof(piiDetector));

        _pdfRedactor = pdfRedactor
            ?? throw new ArgumentNullException(nameof(pdfRedactor));

        _pdfRasterizer = pdfRasterizer
            ?? throw new ArgumentNullException(nameof(pdfRasterizer));

        _logger = logger ?? NullLogger<PdfPiiSanitizer>.Instance;
    }

    public Task<SanitizePiiResponse> SanitizePiiAsync(
        SanitizePiiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var operation = TimedLogOperation.Start(_logger, nameof(SanitizePiiAsync));
        ValidateRequest(request);

        cancellationToken.ThrowIfCancellationRequested();

        var extractedText = _pdfTextExtractor.ExtractPdfText(
            new ExtractPdfTextRequest
            {
                PdfData = request.PdfData
            });

        cancellationToken.ThrowIfCancellationRequested();

        var detectRequest = new DetectPiiRequest
        {
            PdfText = extractedText
        };

        var piiDetectionItems = request.PiiValues.Count > 0
            ? _piiDetector.DetectPii2(new DetectPii2Request
            {
                FullText = extractedText.FullText,
                PiiValues = request.PiiValues
            }).PiiItems
            : _piiDetector.DetectPii(detectRequest).PiiItems;

        var piiDetection = new DetectPiiResponse
        {
            PiiItems = piiDetectionItems
        };

        _logger.LogInformation(
            "PII detection completed with {PiiItemCount} items",
            piiDetection.PiiItems.Count);

        if (!piiDetection.PiiDetected)
        {
            return Task.FromResult(CreateRasterizedResponse(
                request.PdfData,
                PiiDetectionStatus.NotDetected,
                []));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var redactionResponse = _pdfRedactor.RedactPdf(
            new RedactPdfRequest
            {
                PdfData = request.PdfData,
                PiiItems = piiDetection.PiiItems
            });

        ValidateRedactionResponse(redactionResponse);

        cancellationToken.ThrowIfCancellationRequested();

        var verificationText = _pdfTextExtractor.ExtractPdfText(
            new ExtractPdfTextRequest
            {
                PdfData = redactionResponse.PdfData
            });

        VerifyRedactions(
            piiDetection.PiiItems,
            verificationText);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(CreateRasterizedResponse(
            redactionResponse.PdfData,
            PiiDetectionStatus.DetectedAndRemoved,
            piiDetection.PiiItems));
    }

    private SanitizePiiResponse CreateRasterizedResponse(
        byte[] verifiedPdfData,
        PiiDetectionStatus piiStatus,
        IReadOnlyList<PiiItem> piiItems)
    {
        using var operation = TimedLogOperation.Start(_logger, nameof(CreateRasterizedResponse));
        var rasterized = _pdfRasterizer.RasterizePdf(
            new RasterizePdfRequest { PdfData = verifiedPdfData });

        if (rasterized.PdfData is null || rasterized.PdfData.Length == 0)
        {
            throw new PdfPiiRedactionException(
                "The PDF rasterizer returned an empty document.");
        }

        return new SanitizePiiResponse
        {
            RasterizedPdfData = rasterized.PdfData,
            PageCount = rasterized.PageCount,
            RasterizationDpi = rasterized.Dpi,
            Sha256Hash = Convert.ToHexString(SHA256.HashData(rasterized.PdfData)),
            PiiStatus = piiStatus,
            PiiItems = piiItems
        };
    }

    private static void ValidateRequest(SanitizePiiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PdfData is null)
        {
            throw new ArgumentException(
                "PDF data cannot be null.",
                nameof(request));
        }

        if (request.PdfData.Length == 0)
        {
            throw new ArgumentException(
                "PDF data cannot be empty.",
                nameof(request));
        }
    }

    private static void ValidateRedactionResponse(
        RedactPdfResponse response)
    {
        if (response is null)
        {
            throw new PdfPiiRedactionException(
                "The PDF redactor returned no response.");
        }

        if (response.PdfData is null ||
            response.PdfData.Length == 0)
        {
            throw new PdfPiiRedactionException(
                "The PDF redactor returned an empty document.");
        }
    }

    private static void VerifyRedactions(
        IReadOnlyList<PiiItem> piiItems,
        ExtractPdfTextResponse verificationText)
    {
        ArgumentNullException.ThrowIfNull(piiItems);
        ArgumentNullException.ThrowIfNull(verificationText);

        foreach (var piiItem in piiItems)
        {
            if (string.IsNullOrWhiteSpace(piiItem.Value))
            {
                continue;
            }

            if (verificationText.FullText.Contains(
                    piiItem.Value,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new PdfPiiRedactionException(
                    $"PII redaction verification failed. " +
                    $"A value classified as '{piiItem.Type}' " +
                    "is still present in the resulting PDF.");
            }
        }
    }
}
