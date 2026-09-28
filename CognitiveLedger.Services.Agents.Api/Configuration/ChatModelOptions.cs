namespace CognitiveLedger.Services.Agents.Api.Configuration;

public sealed class ChatModelOptions
{
    public const string SectionName = "ChatModel";
    public const string AnthropicProvider = "Anthropic";
    public const string OllamaProvider = "Ollama";

    public required string Provider { get; init; }
}
