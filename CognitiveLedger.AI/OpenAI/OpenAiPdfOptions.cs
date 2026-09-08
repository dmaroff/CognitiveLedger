namespace CognitiveLedger.AI.OpenAI;

public sealed class OpenAiPdfOptions
{
    public required string ApiKey { get; init; }

    public string Model { get; init; } = "gpt-5-mini"; //"gpt-4o-mini";

    public Uri Endpoint { get; init; } = new("https://api.openai.com/v1/responses");
}
