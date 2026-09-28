using CognitiveLedger.Common.Request;
using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.Parser.PDF.Request;

public sealed class RedactPdfRequest : RequestBase
{
    public required byte[] PdfData { get; init; }

    public required IReadOnlyCollection<PiiItem> PiiItems { get; init; }

    public required IReadOnlyCollection<PdfTextReplacement> Replacements { get; init; }
}
