namespace CognitiveLedger.AI.OpenAI.Request;

public interface IExtractPdfStatementRequest
{
    byte[] PdfData { get; set; }

    string SummaryPrompt { get; }

    object SummarySchema { get; }

    string TransactionPrompt { get; }

    object TransactionSchema { get; }
}