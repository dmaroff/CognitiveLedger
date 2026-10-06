namespace CognitiveLedger.Parser.PDF.Dtos;

public sealed record PdfPageText
{
    public required int PageNumber { get; init; }

    public required string Text { get; init; }
}
