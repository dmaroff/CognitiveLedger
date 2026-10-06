namespace CognitiveLedger.Statements.Abstractions;

public interface IStatementDefinitionResolver
{
    IStatementDefinition? Resolve(StatementDefinitionSelector selector);
}
