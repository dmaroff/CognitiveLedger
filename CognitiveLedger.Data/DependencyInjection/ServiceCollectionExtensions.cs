using System;
using CognitiveLedger.Data.Database;
using CognitiveLedger.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitiveLedgerData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        const string connectionStringName = "CognitiveLedger";
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not configured.");

        return services.AddCognitiveLedgerData(connectionString);
    }

    private static IServiceCollection AddCognitiveLedgerData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CognitiveLedgerDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IStatementRepository, StatementRepository>();
        services.AddScoped<IStatementProcessingRepository, StatementProcessingRepository>();

        return services;
    }
}
