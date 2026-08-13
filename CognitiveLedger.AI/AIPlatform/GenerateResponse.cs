using System.Text.Json.Serialization;

namespace CognitiveLedger.AI.AIPlatform;

public sealed class GenerateResponse
{
    [JsonPropertyName("response")]
    public required string Response { get; init; }
}