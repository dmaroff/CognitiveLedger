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

    /// <summary>
    /// Either the string "json" (any valid JSON) or a JSON Schema object that constrains
    /// Ollama's structured output to an exact shape.
    /// </summary>
    [JsonPropertyName("format")]
    public object? Format { get; init; }

    /// <summary>
    /// For hybrid-reasoning models (e.g. qwen3): set false to skip the model's internal
    /// "thinking" phase and go straight to the answer, since that phase can dominate
    /// generation time on tasks like structured extraction.
    /// </summary>
    [JsonPropertyName("think")]
    public bool? Think { get; init; }

    /// <summary>
    /// Runtime model options, e.g. <c>new { num_ctx = 16384 }</c>. Ollama defaults num_ctx to a
    /// small value (commonly 2048-4096 tokens) regardless of what the model itself supports;
    /// anything beyond that in the prompt is silently dropped rather than erroring, so longer
    /// documents need this set explicitly.
    /// </summary>
    [JsonPropertyName("options")]
    public object? Options { get; init; }
}