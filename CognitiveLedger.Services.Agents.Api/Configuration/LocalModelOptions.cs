namespace CognitiveLedger.Services.Agents.Api.Configuration;

public sealed class LocalModelOptions
{
    public const string SectionName = "Ollama";

    public required Uri Endpoint { get; init; }
    public required string ModelName { get; init; }
}
