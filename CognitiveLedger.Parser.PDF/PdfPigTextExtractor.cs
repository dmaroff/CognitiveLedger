using System.Text;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using UglyToad.PdfPig;

namespace CognitiveLedger.Parser.PDF;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public ExtractPdfTextResponse ExtractPdfText( ExtractPdfTextRequest request)
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

        using var stream = new MemoryStream(
            request.PdfData,
            writable: false);

        using var document = PdfDocument.Open(stream);

        return ExtractText(document);
    }

    private static ExtractPdfTextResponse ExtractText(PdfDocument document)
    {
        var sb = new StringBuilder();
        var textItems = new List<PdfTextItem>();

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();

            //
            // Keep every word and its physical location.
            //
            // This information will later be used by the PII detector
            // and PDF redaction engine.
            //
            textItems.AddRange(words.Select(
                word => new PdfTextItem
                    {
                        Text = word.Text,
                        PageNumber = page.Number,
                        Bounds = new PdfRectangle
                            { 
                                X = word.BoundingBox.Left,
                                Y = word.BoundingBox.Bottom,
                                Width = word.BoundingBox.Width,
                                Height = word.BoundingBox.Height
                                
                            }
                    }));

            //
            // PdfPig's Page.Text runs the entire page together.
            //
            // Reconstruct the visual lines by grouping words on roughly
            // the same Y position and then ordering those words from
            // left to right.
            //
            var lines = words
                .GroupBy(
                    word =>
                        Math.Round(
                            word.BoundingBox.Bottom / 3) * 3)
                .OrderByDescending(line => line.Key)
                .Select(
                    line =>
                        string.Join(
                            " ",
                            line
                                .OrderBy(
                                    word =>
                                        word.BoundingBox.Left)
                                .Select(
                                    word =>
                                        word.Text)));

            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
        }

        return new ExtractPdfTextResponse
        {
            FullText = sb.ToString(),
            TextItems = textItems
        };
    }
}