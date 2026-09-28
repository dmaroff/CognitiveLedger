namespace CognitiveLedger.Parser.PDF.Dtos;

public sealed class PdfTextReplacement
{
    public required string OriginalValue { get; init; }

    public required string ReplacementValue { get; init; }
}
