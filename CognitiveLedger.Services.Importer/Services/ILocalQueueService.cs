using CognitiveLedger.Services.Importer.Request;

namespace CognitiveLedger.Services.Importer.Services;

public interface ILocalQueueService
{
    void Enqueue(ImportRequest request, long auditId);
}