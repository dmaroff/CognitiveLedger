using System.ComponentModel.DataAnnotations;

namespace CognitiveLedger.Data.Models;

public sealed class AiModel
{
    public long Id { get; init; }
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;
    public long AiProviderId { get; init; }
    public AiProvider? AiProvider { get; init; }
}

public static class AiModelCatalog
{
    public const long Gpt56TerraId = 1;
}
