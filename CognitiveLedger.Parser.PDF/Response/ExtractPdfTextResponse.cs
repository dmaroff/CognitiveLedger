using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.Parser.PDF.Response;

public sealed class ExtractPdfTextResponse
{
    /// <summary>
    /// Extracted text grouped by source PDF page.
    /// </summary>
    public required IReadOnlyList<PdfPageText> Pages { get; init; }

    /// <summary>
    /// All extracted text from the document.
    /// </summary>
    public required string FullText { get; init; }

    /// <summary>
    /// Individual text elements including their locations.
    /// </summary>
    public required IReadOnlyList<PdfTextItem> TextItems { get; init; }
}
