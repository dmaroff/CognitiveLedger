using CognitiveLedger.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitiveLedgerData(this IServiceCollection services, string sqliteConnectionString)
    {
        services.AddDbContext<CognitiveLedgerDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        return services;
    }
}
