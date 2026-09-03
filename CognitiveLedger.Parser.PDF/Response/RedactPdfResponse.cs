namespace CognitiveLedger.Parser.PDF.Response;

public sealed class RedactPdfResponse
{
    public required byte[] PdfData { get; init; }
}