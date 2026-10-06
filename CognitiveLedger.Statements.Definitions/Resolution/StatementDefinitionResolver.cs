using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Statements.Definitions.Resolution;

public sealed class StatementDefinitionResolver : IStatementDefinitionResolver
{
    private readonly IReadOnlyList<IStatementDefinition> _definitions;

    public StatementDefinitionResolver(IEnumerable<IStatementDefinition> definitions)
    {
        _definitions = [.. definitions];

        var duplicateKey = _definitions
            .GroupBy(
                definition => definition.Descriptor.Key,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateKey is not null)
        {
            throw new InvalidOperationException(
                $"Statement definition key '{duplicateKey.Key}' is registered more than once.");
        }
    }

    public IStatementDefinition? Resolve(StatementDefinitionSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var candidates = _definitions
            .Where(definition =>
                definition.Descriptor.StatementKind == selector.StatementKind)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(selector.DefinitionKey))
        {
            return candidates.SingleOrDefault(definition => string.Equals(
                definition.Descriptor.Key,
                selector.DefinitionKey,
                StringComparison.OrdinalIgnoreCase));
        }

        if (!IsUnknownSource(selector.SourceName))
        {
            var sourceMatches = candidates
                .Where(definition => MatchesSource(
                    definition.Descriptor,
                    selector.SourceName))
                .ToArray();

            return sourceMatches.Length == 1 ? sourceMatches[0] : null;
        }

        // Preserve the original single-definition behavior. Once multiple definitions exist
        // for a statement type, callers must identify the source or provide a definition key.
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static bool MatchesSource(
        StatementDefinitionDescriptor descriptor,
        string sourceName) =>
        string.Equals(descriptor.Key, sourceName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(descriptor.Institution, sourceName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(descriptor.Product, sourceName, StringComparison.OrdinalIgnoreCase) ||
        descriptor.SourceAliases.Contains(sourceName, StringComparer.OrdinalIgnoreCase);

    private static bool IsUnknownSource(string sourceName) =>
        string.IsNullOrWhiteSpace(sourceName) ||
        string.Equals(sourceName, "Unknown", StringComparison.OrdinalIgnoreCase);
}
