namespace CognitiveLedger.AI.OpenAI;

public sealed class OpenAiPdfStatementException : Exception
{
    public OpenAiPdfStatementException(string message)
        : base(message)
    {
    }

    public OpenAiPdfStatementException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
