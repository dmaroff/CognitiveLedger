using CognitiveLedger.Common.Response;

namespace CognitiveLedger.Services.Importer.Response;

public sealed class ImportPdfResponse : ResponseBase
{
    public Guid JobId { get; init; }
    public long? StatementId { get; init; }
}
