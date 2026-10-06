using CognitiveLedger.Statements.Abstractions;
using CognitiveLedger.Statements.Definitions.CapitalOneBjs;
using CognitiveLedger.Statements.Definitions.CapitalOneDiscover;
using CognitiveLedger.Statements.Definitions.Resolution;
using CognitiveLedger.Statements.Definitions.SynchronyAmazon;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Statements.Definitions.DependencyInjection;

public static class StatementDefinitionServiceCollectionExtensions
{
    public static IServiceCollection AddStatementDefinitions(this IServiceCollection services)
    {
        services.AddSingleton<IStatementDefinition, SynchronyAmazonStatementDefinition>();
        services.AddSingleton<IStatementDefinition, CapitalOneBjsStatementDefinition>();
        services.AddSingleton<IStatementDefinition, CapitalOneDiscoverStatementDefinition>();
        services.AddSingleton<IStatementDefinitionResolver, StatementDefinitionResolver>();
        return services;
    }
}
