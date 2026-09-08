namespace CognitiveLedger.Parser.PDF.Request;

public sealed class RasterizePdfRequest
{
    public required byte[] PdfData { get; init; }

    public int Dpi { get; init; } = 200;
}
