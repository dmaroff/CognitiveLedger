namespace CognitiveLedger.Statements.Abstractions;

public sealed record StatementDefinitionSelector(
    string? DefinitionKey,
    string SourceName,
    StatementKind StatementKind);
