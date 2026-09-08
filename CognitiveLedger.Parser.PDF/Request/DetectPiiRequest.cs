using CognitiveLedger.Parser.PDF.Response;

namespace CognitiveLedger.Parser.PDF.Request;

public sealed class DetectPiiRequest
{
    public required ExtractPdfTextResponse PdfText { get; init; }
}
