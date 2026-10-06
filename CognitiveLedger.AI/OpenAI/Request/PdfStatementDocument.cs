using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.AI.OpenAI.Request;

public sealed record PdfStatementDocument
{
    public required byte[] RasterizedPdfData { get; init; }

    public required IReadOnlyList<PdfPageText> SanitizedPageText { get; init; }
}
