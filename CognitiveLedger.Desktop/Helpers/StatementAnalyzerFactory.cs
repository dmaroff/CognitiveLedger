using System;
using CognitiveLedger.AI.Analyzers;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Desktop.Helpers;

public interface IStatementAnalyzerFactory
{
    IStatementAnalyzer GetAnalyzer(string bankKey);
}

public class StatementAnalyzerFactory : IStatementAnalyzerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public StatementAnalyzerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IStatementAnalyzer GetAnalyzer(string bankKey)
    {
        // Fetches the transient instance by its registration key
        var analyzer = _serviceProvider.GetKeyedService<IStatementAnalyzer>(bankKey);
        
        return analyzer ?? throw new ArgumentException($"No analyzer registered for bank: {bankKey}");
    }
}
