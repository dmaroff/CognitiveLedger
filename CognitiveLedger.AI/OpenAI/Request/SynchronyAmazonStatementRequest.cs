using CognitiveLedger.AI.OpenAI.StatementDefinitions;

namespace CognitiveLedger.AI.OpenAI.Request;

public sealed class SynchronyAmazonStatementRequest : IExtractPdfStatementRequest
{
    public required byte[] PdfData { get; set; }

    public string SummaryPrompt => SynchronyAmazonStatementDefinition.SummaryPrompt;

    public object SummarySchema => SynchronyAmazonStatementDefinition.SummarySchema;

    public string TransactionPrompt => SynchronyAmazonStatementDefinition.TransactionPrompt;

    public object TransactionSchema => SynchronyAmazonStatementDefinition.TransactionSchema;
}
