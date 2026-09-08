using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using CognitiveLedger.Parser.PDF.Types;

namespace CognitiveLedger.Parser.PDF.Interfaces;

public interface IPiiDetector
{
    DetectPiiResponse DetectPii(DetectPiiRequest request);

    DetectPii2Response DetectPii2(DetectPii2Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FullText);
        ArgumentNullException.ThrowIfNull(request.PiiValues);

        var piiItems = request.PiiValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(value => request.FullText.Contains(
                value,
                StringComparison.OrdinalIgnoreCase))
            .Select(value => new PiiItem
            {
                Type = PiiType.Unknown,
                Value = value,
                PageNumber = 0,
                Bounds = []
            })
            .ToArray();

        return new DetectPii2Response { PiiItems = piiItems };
    }

    IReadOnlyCollection<PiiItem> PiiItems { get; }
}
