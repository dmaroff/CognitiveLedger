namespace CognitiveLedger.AI.OpenAI.Request;

public sealed class ExtractPdfStatementRequest
{
    /// <summary>
    /// Complete sanitized, rasterized PDF bytes to submit to OpenAI.
    /// </summary>
    public required byte[] PdfData { get; init; }

    public required string SummaryPrompt { get; init; }

    public required object SummarySchema { get; init; }

    public required string TransactionPrompt { get; init; }

    public required object TransactionSchema { get; init; }
}
