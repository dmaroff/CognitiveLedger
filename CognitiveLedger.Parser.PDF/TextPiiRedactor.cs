using System.Text.RegularExpressions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using iText.Kernel.Pdf;
using iText.PdfCleanup;
using iText.PdfCleanup.Autosweep;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.Parser.PDF;

public sealed class TextPdfRedactor : IPdfRedactor
{
    private readonly ILogger<TextPdfRedactor> _logger;

    public TextPdfRedactor(ILogger<TextPdfRedactor>? logger = null)
    {
        _logger = logger ?? NullLogger<TextPdfRedactor>.Instance;
    }

    public RedactPdfResponse RedactPdf(RedactPdfRequest request)
    {
        using var operation = TimedLogOperation.Start(_logger, nameof(RedactPdf));
        ArgumentNullException.ThrowIfNull(request);

        if (request.PdfData.Length == 0)
        {
            throw new ArgumentException(
                "PDF data cannot be empty.",
                nameof(request));
        }

        var strategy = new CompositeCleanupStrategy();

        foreach (var piiItem in request.PiiItems)
        {
            if (string.IsNullOrWhiteSpace(piiItem.Value))
            {
                continue;
            }
            var escapedValue = Regex.Escape(piiItem.Value);
            strategy.Add(new RegexBasedCleanupStrategy($"(?i:{escapedValue})"));
        }

        using var inputStream =
            new MemoryStream(request.PdfData, writable: false);

        using var outputStream =
            new MemoryStream();

        using (var pdfDocument = new PdfDocument(
                   new PdfReader(inputStream),
                   new PdfWriter(outputStream)))
        {
            PdfCleaner.AutoSweepCleanUp(
                pdfDocument,
                strategy);
        }

        var response = new RedactPdfResponse
        {
            PdfData = outputStream.ToArray()
        };

        _logger.LogInformation(
            "Redacted {PiiItemCount} PII items; output contains {PdfByteCount} bytes",
            request.PiiItems.Count,
            response.PdfData.Length);
        return response;
    }
}
