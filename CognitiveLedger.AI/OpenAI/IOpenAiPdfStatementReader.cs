using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;

namespace CognitiveLedger.AI.OpenAI;

public interface IOpenAiPdfStatementReader
{
    Task<ExtractPdfStatementResponse> ExtractAsync(
        ExtractPdfStatementRequest request,
        CancellationToken cancellationToken = default);
}
