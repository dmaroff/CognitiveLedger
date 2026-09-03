using CognitiveLedger.Parser.PDF.Types;

namespace CognitiveLedger.Parser.PDF.Dtos;

public sealed class PiiItem
{
    public required PiiType Type { get; init; }

    public required string Value { get; init; }

    public required int PageNumber { get; init; }

    public required IReadOnlyList<PdfRectangle> Bounds { get; init; }
}