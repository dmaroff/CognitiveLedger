using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;

namespace CognitiveLedger.Data.Database;

public class CognitiveLedgerDbContext(DbContextOptions<CognitiveLedgerDbContext> options)
    : DbContext(options)
{
    public DbSet<CreditCardAccount> CreditCardAccounts => Set<CreditCardAccount>();
    public DbSet<CreditCardStatement> CreditCardStatements => Set<CreditCardStatement>();
    public DbSet<CreditCardTransaction> CreditCardTransactions => Set<CreditCardTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCardAccount>(entity =>
        {
            entity.Property(a => a.IssuerName).IsRequired();
            entity.Property(a => a.LastFourDigits).IsRequired().HasMaxLength(4);
            entity.Property(a => a.CurrencyCode).IsRequired().HasMaxLength(3);
        });

        modelBuilder.Entity<CreditCardStatement>(entity =>
        {
            entity.Property(s => s.SourceFileName).IsRequired();
            entity.Property(s => s.SourceFileHash).IsRequired();
            entity.Property(s => s.ImportStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(s => s.SourceFileHash);

            entity.HasOne(s => s.Account)
                .WithMany(a => a.Statements)
                .HasForeignKey(s => s.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreditCardTransaction>(entity =>
        {
            entity.Property(t => t.RawDescription).IsRequired();
            entity.Property(t => t.CurrencyCode).IsRequired().HasMaxLength(3);
            entity.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.ReviewStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(t => t.Statement)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.StatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
