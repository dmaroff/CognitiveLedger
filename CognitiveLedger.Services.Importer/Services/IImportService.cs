using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;

namespace CognitiveLedger.Services.Importer.Services;

public interface IImportService
{
    Task<ImportResponse> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);
}
