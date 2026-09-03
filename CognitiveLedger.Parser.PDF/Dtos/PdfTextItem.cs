namespace CognitiveLedger.Parser.PDF.Dtos;

public sealed record PdfTextItem
{
    public required string Text { get; init; }

    public required int PageNumber { get; init; }

    public required PdfRectangle Bounds { get; init; }
}