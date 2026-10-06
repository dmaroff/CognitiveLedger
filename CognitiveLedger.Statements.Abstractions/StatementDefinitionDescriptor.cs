namespace CognitiveLedger.Statements.Abstractions;

public sealed record StatementDefinitionDescriptor(
    string Key,
    string Institution,
    string Product,
    StatementKind StatementKind,
    IReadOnlyList<string> SourceAliases,
    IReadOnlyList<string> IdentificationMarkers);
