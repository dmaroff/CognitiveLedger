using System.Text.RegularExpressions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using iText.Kernel.Pdf;
using iText.PdfCleanup;
using iText.PdfCleanup.Autosweep;

namespace CognitiveLedger.Parser.PDF;

public sealed class TextPdfRedactor : IPdfRedactor
{
    public RedactPdfResponse RedactPdf(RedactPdfRequest request)
    {
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

            strategy.Add(
                new RegexBasedCleanupStrategy(
                    Regex.Escape(piiItem.Value)));
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

        return new RedactPdfResponse
        {
            PdfData = outputStream.ToArray()
        };
    }
}