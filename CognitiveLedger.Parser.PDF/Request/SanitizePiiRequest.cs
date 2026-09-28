using CognitiveLedger.Common.Request;
using CognitiveLedger.Parser.PDF.Dtos;

namespace CognitiveLedger.Parser.PDF.Request;

/// <summary>
/// Requests complete PII sanitization of a PDF document.
/// </summary>
public sealed class SanitizePiiRequest : RequestBase
{
    /// <summary>
    /// Original PDF data to inspect and sanitize.
    /// </summary>
    public required byte[] PdfData { get; init; }

    /// <summary>
    /// Optional explicit values to find case-insensitively and redact wherever
    /// they occur. When supplied, automatic PII detection is bypassed.
    /// </summary>
    public IList<string> PiiValues { get; init; } = [];

    /// <summary>
    /// Text values to replace in the PDF before it is rasterized and sent to
    /// an external service.
    /// </summary>
    public IList<PdfTextReplacement> Replacements { get; init; } = [];
}
