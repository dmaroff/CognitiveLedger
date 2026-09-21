using System.ComponentModel.DataAnnotations;

namespace CognitiveLedger.Data.Models;

public sealed class AiProvider
{
    public long Id { get; init; }
    
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;
}

public static class AiProviderCatalog
{
    public const long OpenAiId = 1;
}
