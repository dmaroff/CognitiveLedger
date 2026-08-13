namespace CognitiveLedger.AI.AIPlatform;

public class AiGenerateException : Exception
{
    public AiGenerateException(string message)
        : base(message)
    {
    }
    
    public AiGenerateException(Exception innerException)
        : base("An error occurred while generating a response from the AI platform.", innerException)
    {
    }
}