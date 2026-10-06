using CognitiveLedger.Common.Response;

namespace CognitiveLedger.Services.Importer.Response;

public sealed class ImportPdfResponse : ResponseBase
{
    public bool Existing { get; init; }
    public long? ProcessingAuditId { get; init; }
    public long? StatementId { get; init; }
}
