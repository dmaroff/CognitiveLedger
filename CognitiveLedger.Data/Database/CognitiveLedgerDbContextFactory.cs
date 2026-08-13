using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CognitiveLedger.Data.Database;

// Used only by `dotnet ef` design-time tooling to create migrations; not used at app runtime.
public class CognitiveLedgerDbContextFactory : IDesignTimeDbContextFactory<CognitiveLedgerDbContext>
{
    public CognitiveLedgerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CognitiveLedgerDbContext>();
        optionsBuilder.UseSqlite("Data Source=design-time.db");
        return new CognitiveLedgerDbContext(optionsBuilder.Options);
    }
}
