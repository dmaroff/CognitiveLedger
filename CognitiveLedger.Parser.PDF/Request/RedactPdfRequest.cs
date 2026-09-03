using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.Parser.PDF.Request;

public sealed class RedactPdfRequest
{
    public required byte[] PdfData { get; init; }

    public required IReadOnlyCollection<PiiItem> PiiItems { get; init; }
}