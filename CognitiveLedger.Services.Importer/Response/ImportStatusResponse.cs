namespace CognitiveLedger.Services.Importer.Response;

public sealed class ImportStatusResponse
{
    public required long ProcessingAuditId { get; init; }
    public required ImportProcessingStatus Status { get; init; }
    public long? StatementId { get; init; }
    public int? ExtractedTransactionCount { get; init; }
    public string? ErrorMessage { get; init; }
}
