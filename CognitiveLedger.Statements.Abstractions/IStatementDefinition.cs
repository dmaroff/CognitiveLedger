namespace CognitiveLedger.Statements.Abstractions;

public interface IStatementDefinition
{
    StatementDefinitionDescriptor Descriptor { get; }

    string SummaryPrompt { get; }

    object SummarySchema { get; }

    string TransactionPrompt { get; }

    object TransactionSchema { get; }
}
