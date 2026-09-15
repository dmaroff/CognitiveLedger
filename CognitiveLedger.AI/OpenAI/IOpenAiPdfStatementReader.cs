using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;

namespace CognitiveLedger.AI.OpenAI;

public interface IOpenAiPdfStatementReader
{
    Task<ExtractPdfStatementResponse> ExtractAsync(
        IExtractPdfStatementRequest request,
        CancellationToken cancellationToken = default);
}
