using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.AI.OpenAI;

public interface IOpenAiPdfStatementReader
{
    Task<ExtractPdfStatementResponse> ExtractAsync(
        PdfStatementDocument document,
        IStatementDefinition definition,
        CancellationToken cancellationToken = default);
}
