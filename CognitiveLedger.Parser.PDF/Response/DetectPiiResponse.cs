using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.Parser.PDF.Response;

public sealed class DetectPiiResponse
{
    public required IReadOnlyList<PiiItem> PiiItems { get; init; }

    public bool PiiDetected => PiiItems.Count > 0;
}