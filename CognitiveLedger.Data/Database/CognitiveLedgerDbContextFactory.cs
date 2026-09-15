using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CognitiveLedger.Data.Database;

// Used only by `dotnet ef` design-time tooling to create migrations; not used at app runtime.
public class CognitiveLedgerDbContextFactory : IDesignTimeDbContextFactory<CognitiveLedgerDbContext>
{
    public CognitiveLedgerDbContext CreateDbContext(string[] args)
    {
        const string connectionStringName = "CognitiveLedger";
        var currentDirectory = Directory.GetCurrentDirectory();
        var dataProjectDirectory = Path.Combine(currentDirectory, "CognitiveLedger.Data");
        var settingsDirectory = Directory.Exists(dataProjectDirectory)
            ? dataProjectDirectory
            : currentDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(settingsDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<CognitiveLedgerDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new CognitiveLedgerDbContext(optionsBuilder.Options);
    }
}
