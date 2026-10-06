using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Exceptions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;
using CognitiveLedger.Common;
using System.Security.Cryptography;

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
    private readonly IAppLog<PdfPiiSanitizer> _logger;

    public PdfPiiSanitizer(
        IPdfTextExtractor pdfTextExtractor,
        IPiiDetector piiDetector,
        IPdfRedactor pdfRedactor,
        IPdfRasterizer pdfRasterizer,
        IAppLog<PdfPiiSanitizer> logger)
    {
        _pdfTextExtractor = pdfTextExtractor
            ?? throw new ArgumentNullException(nameof(pdfTextExtractor));

        _piiDetector = piiDetector
            ?? throw new ArgumentNullException(nameof(piiDetector));

        _pdfRedactor = pdfRedactor
            ?? throw new ArgumentNullException(nameof(pdfRedactor));

        _pdfRasterizer = pdfRasterizer
            ?? throw new ArgumentNullException(nameof(pdfRasterizer));

        _logger = logger;
    }

    public Task<SanitizePiiResponse> SanitizePiiAsync(
        SanitizePiiRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogMethodStart(request);
        ValidateRequest(request);

        cancellationToken.ThrowIfCancellationRequested();

        var extractedText = _pdfTextExtractor.ExtractPdfText(
            new ExtractPdfTextRequest
            {
                UserId = request.UserId,
                PdfData = request.PdfData
            });

        cancellationToken.ThrowIfCancellationRequested();

        var detectRequest = new DetectPiiRequest
        {
            PdfText = extractedText
        };

        var piiDetectionItems = request.Replacements.Count > 0
            ? _piiDetector.DetectPii2(new DetectPii2Request
            {
                FullText = extractedText.FullText,
                PiiValues =
                [
                    .. request.Replacements
                        .Select(replacement => replacement.OriginalValue)
                ]
            }).PiiItems
            : _piiDetector.DetectPii(detectRequest).PiiItems;

        var piiDetection = new DetectPiiResponse
        {
            PiiItems = piiDetectionItems
        };
        var sanitizedPageText = SanitizePageText(
            extractedText.Pages,
            request.Replacements);

        _logger.LogInfo(request, $"PII detection completed with {piiDetection.PiiItems.Count} items");

        if (!piiDetection.PiiDetected)
        {
            return Task.FromResult(CreateRasterizedResponse(
                request.PdfData,
                sanitizedPageText,
                PiiDetectionStatus.NotDetected,
                []));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var redactionResponse = _pdfRedactor.RedactPdf(
            new RedactPdfRequest
            {
                UserId = request.UserId,
                PdfData = request.PdfData,
                PiiItems = piiDetection.PiiItems,
                Replacements = [.. request.Replacements]
            });

        ValidateRedactionResponse(redactionResponse);

        cancellationToken.ThrowIfCancellationRequested();

        var verificationText = _pdfTextExtractor.ExtractPdfText(
            new ExtractPdfTextRequest
            {
                UserId = request.UserId,
                PdfData = redactionResponse.PdfData
            });

        VerifyRedactions(
            piiDetection.PiiItems,
            request.Replacements,
            verificationText);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogMethodEnd(request);
        
        return Task.FromResult(CreateRasterizedResponse(
            redactionResponse.PdfData,
            sanitizedPageText,
            PiiDetectionStatus.DetectedAndRemoved,
            piiDetection.PiiItems));
    }

    private SanitizePiiResponse CreateRasterizedResponse(
        byte[] verifiedPdfData,
        IReadOnlyList<PdfPageText> sanitizedPageText,
        PiiDetectionStatus piiStatus,
        IReadOnlyList<PiiItem> piiItems)
    {
        _logger.LogMethodStart();
        var rasterized = _pdfRasterizer.RasterizePdf(
            new RasterizePdfRequest { PdfData = verifiedPdfData });

        if (rasterized.PdfData is null || rasterized.PdfData.Length == 0)
        {
            throw new PdfPiiRedactionException(
                "The PDF rasterizer returned an empty document.");
        }

        _logger.LogMethodEnd();
        return new SanitizePiiResponse
        {
            RasterizedPdfData = rasterized.PdfData,
            SanitizedPageText = sanitizedPageText,
            PageCount = rasterized.PageCount,
            RasterizationDpi = rasterized.Dpi,
            Sha256Hash = Convert.ToHexString(SHA256.HashData(rasterized.PdfData)),
            PiiStatus = piiStatus,
            PiiItems = piiItems
        };
    }

    private static IReadOnlyList<PdfPageText> SanitizePageText(
        IReadOnlyList<PdfPageText> pages,
        IEnumerable<PdfTextReplacement> replacements)
    {
        PdfTextReplacement[] orderedReplacements =
        [
            .. replacements
                .Where(replacement =>
                    !string.IsNullOrWhiteSpace(replacement.OriginalValue) &&
                    !string.IsNullOrWhiteSpace(replacement.ReplacementValue))
                .OrderByDescending(replacement => replacement.OriginalValue.Length)
        ];

        return
        [
            .. pages.Select(page => new PdfPageText
            {
                PageNumber = page.PageNumber,
                Text = orderedReplacements.Aggregate(
                    page.Text,
                    (text, replacement) => text.Replace(
                        replacement.OriginalValue,
                        replacement.ReplacementValue,
                        StringComparison.OrdinalIgnoreCase))
            })
        ];
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
        IList<PdfTextReplacement> replacements,
        ExtractPdfTextResponse verificationText)
    {
        ArgumentNullException.ThrowIfNull(piiItems);
        ArgumentNullException.ThrowIfNull(replacements);
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

            var replacement = replacements.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.OriginalValue,
                    piiItem.Value,
                    StringComparison.OrdinalIgnoreCase));

            if (replacement is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(replacement.ReplacementValue) ||
                !verificationText.FullText.Contains(
                    replacement.ReplacementValue,
                    StringComparison.Ordinal))
            {
                throw new PdfPiiRedactionException(
                    "PII replacement verification failed. " +
                    $"The replacement token for a value classified as '{piiItem.Type}' " +
                    "is missing from the resulting PDF.");
            }
        }
    }
}
