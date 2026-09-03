namespace CognitiveLedger.Parser.PDF.Dtos;

public sealed record PdfRectangle
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}