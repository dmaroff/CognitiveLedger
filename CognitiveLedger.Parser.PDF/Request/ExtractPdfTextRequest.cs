namespace CognitiveLedger.Parser.PDF.Request;

public class ExtractPdfTextRequest
{
    public required byte[] PdfData { get; init; }
}