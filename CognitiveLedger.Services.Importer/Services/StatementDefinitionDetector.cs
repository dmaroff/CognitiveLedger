using System.Text;
using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Services.Importer.Services;

public sealed class StatementDefinitionDetector : IStatementDefinitionDetector
{
    private readonly IReadOnlyList<IStatementDefinition> _definitions;

    public StatementDefinitionDetector(IEnumerable<IStatementDefinition> definitions)
    {
        _definitions = [.. definitions];
    }

    public IStatementDefinition? Detect(
        string statementText,
        StatementKind statementKind)
    {
        if (string.IsNullOrWhiteSpace(statementText))
        {
            return null;
        }

        var normalizedText = Normalize(statementText);
        var matches = _definitions
            .Where(definition =>
                definition.Descriptor.StatementKind == statementKind &&
                definition.Descriptor.IdentificationMarkers.Any(marker =>
                    normalizedText.Contains(
                        Normalize(marker),
                        StringComparison.Ordinal)))
            .ToArray();

        return matches.Length == 1 ? matches[0] : null;
    }

    private static string Normalize(string value)
    {
        var normalized = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(char.ToUpperInvariant(character));
            }
        }

        return normalized.ToString();
    }
}
