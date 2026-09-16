using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Parser.PDF.Request;

public class ExtractPdfTextRequest : RequestBase
{
    public required byte[] PdfData { get; init; }
}