namespace CognitiveLedger.AI.Analyzers;

public interface IStatementAnalyzer
{
    Task<AnalyzeStatementResponse> AnalyzeStatement(string statementText);
}