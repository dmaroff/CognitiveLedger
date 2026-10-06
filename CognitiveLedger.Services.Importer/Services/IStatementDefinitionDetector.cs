using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Services.Importer.Services;

public interface IStatementDefinitionDetector
{
    IStatementDefinition? Detect(string statementText, StatementKind statementKind);
}
