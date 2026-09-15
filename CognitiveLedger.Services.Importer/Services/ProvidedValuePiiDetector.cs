using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;

namespace CognitiveLedger.Services.Importer.Services;

// The interface's default DetectPii2 implementation matches supplied PII values.
// Do not silently claim that automatic PII detection is available.
public sealed class ProvidedValuePiiDetector : IPiiDetector
{
    public IReadOnlyCollection<PiiItem> PiiItems =>
        throw new NotSupportedException(
            "Use the DetectPii2 result; this detector does not keep shared PII state.");

    public DetectPiiResponse DetectPii(DetectPiiRequest request) =>
        throw new NotSupportedException(
            "Automatic PII detection is not configured. Supply explicit PII values or register a detector.");
}
