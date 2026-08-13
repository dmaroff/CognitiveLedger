namespace CognitiveLedger.AI.Services.Ollama;

public class OllamaSetupException : Exception
{
    public OllamaSetupException(string message)
        : base(message)
    {
    }

    public OllamaSetupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
