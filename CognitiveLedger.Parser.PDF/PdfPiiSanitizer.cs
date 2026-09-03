using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Exceptions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;

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

    public PdfPiiSanitizer(
        IPdfTextExtractor pdfTextExtractor,
        IPiiDetector piiDetector,
        IPdfRedactor pdfRedactor)
    {
        _pdfTextExtractor = pdfTextExtractor
            ?? throw new ArgumentNullException(nameof(pdfTextExtractor));

        _piiDetector = piiDetector
            ?? throw new ArgumentNullException(nameof(piiDetector));

        _pdfRedactor = pdfRedactor
            ?? throw new ArgumentNullException(nameof(pdfRedactor));
    }

    public Task<SanitizePiiResponse> SanitizePiiAsync(
        SanitizePiiRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        cancellationToken.ThrowIfCancellationRequested();

        var extractedText = _pdfTextExtractor.ExtractPdfText(
            new ExtractPdfTextRequest
            {
                PdfData = request.PdfData
            });

        cancellationToken.ThrowIfCancellationRequested();

        var piiDetection = _piiDetector.DetectPii(
            new DetectPiiRequest
            {
                PdfText = extractedText
            });

        if (!piiDetection.PiiDetected)
        {
            return Task.FromResult(
                new SanitizePiiResponse
                {
                    PdfData = request.PdfData,
                    PiiStatus = PiiDetectionStatus.NotDetected,
                    PiiItems = []
                });
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

        return Task.FromResult(
            new SanitizePiiResponse
            {
                PdfData = redactionResponse.PdfData,
                PiiStatus = PiiDetectionStatus.DetectedAndRemoved,
                PiiItems = piiDetection.PiiItems
            });
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