using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Types;

namespace CognitiveLedger.Parser.PDF.Response;

/// <summary>
/// Result of the complete PDF PII sanitization process.
/// </summary>
public sealed class SanitizePiiResponse
{
    /// <summary>
    /// Image-only PDF data created after redaction verification. This is the
    /// artifact that can be previewed and submitted to an external LLM.
    /// </summary>
    public required byte[] RasterizedPdfData { get; init; }

    public required int PageCount { get; init; }

    public required int RasterizationDpi { get; init; }

    public required string Sha256Hash { get; init; }

    /// <summary>
    /// Indicates whether PII was detected and removed.
    /// </summary>
    public required PiiDetectionStatus PiiStatus { get; init; }

    /// <summary>
    /// PII items detected in the original PDF.
    /// Empty when no PII was detected.
    /// </summary>
    public required IReadOnlyList<PiiItem> PiiItems { get; init; }
}
