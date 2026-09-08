namespace CognitiveLedger.Parser.PDF.Request;

public sealed class DetectPii2Request
{
    public required string FullText { get; init; }

    public required IList<string> PiiValues { get; init; }
}
