using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Types;

namespace CognitiveLedger.Parser.PDF.Response;

/// <summary>
/// Result of the complete PDF PII sanitization process.
/// </summary>
public sealed class SanitizePiiResponse
{
    /// <summary>
    /// Sanitized PDF data, or the original data when no PII was detected.
    /// </summary>
    public required byte[] PdfData { get; init; }

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