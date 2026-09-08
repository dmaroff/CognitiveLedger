namespace CognitiveLedger.Parser.PDF.Response;

public sealed class RasterizePdfResponse
{
    public required byte[] PdfData { get; init; }

    public required int PageCount { get; init; }

    public required int Dpi { get; init; }
}
