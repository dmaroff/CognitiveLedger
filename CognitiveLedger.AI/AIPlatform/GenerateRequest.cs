using System.Text.Json.Serialization;

namespace CognitiveLedger.AI.AIPlatform;

public sealed class GenerateRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }
}