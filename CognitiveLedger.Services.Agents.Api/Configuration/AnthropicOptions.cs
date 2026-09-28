namespace CognitiveLedger.Services.Agents.Api.Configuration;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public required string ModelName { get; init; }
    public string? ApiKey { get; init; }
}
