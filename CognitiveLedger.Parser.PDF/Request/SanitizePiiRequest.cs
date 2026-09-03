namespace CognitiveLedger.Parser.PDF.Request;

/// <summary>
/// Requests complete PII sanitization of a PDF document.
/// </summary>
public sealed class SanitizePiiRequest
{
    /// <summary>
    /// Original PDF data to inspect and sanitize.
    /// </summary>
    public required byte[] PdfData { get; init; }
}