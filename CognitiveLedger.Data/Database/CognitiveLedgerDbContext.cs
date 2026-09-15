using CognitiveLedger.Data.Models.CreditCard;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CognitiveLedger.Data.Database;

public class CognitiveLedgerDbContext(DbContextOptions<CognitiveLedgerDbContext> options)
    : DbContext(options)
{
    public DbSet<CreditCardStatement> Statements => Set<CreditCardStatement>();
    public DbSet<CreditCardTransaction> Transactions => Set<CreditCardTransaction>();
    public DbSet<StatementProcessingAudit> ProcessingAudits => Set<StatementProcessingAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCardStatement>(entity =>
        {
            entity.ToTable("statements");
            entity.Property(statement => statement.SourceDocumentSha256)
                .HasColumnName("source_document_sha256")
                .HasMaxLength(64);
            entity.HasIndex(statement => statement.SourceDocumentSha256)
                .IsUnique()
                .HasFilter("source_document_sha256 IS NOT NULL");
            entity.Property(statement => statement.Issuer).IsRequired().HasMaxLength(200);
            entity.Property(statement => statement.AccountName).IsRequired().HasMaxLength(300);
            entity.HasIndex(statement =>
                new { statement.Issuer, statement.AccountName, statement.StatementPeriodEnd });
        });

        modelBuilder.Entity<CreditCardTransaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.Property(transaction => transaction.Category).IsRequired().HasMaxLength(100);
            entity.Property(transaction => transaction.Merchant).IsRequired().HasMaxLength(500);
            entity.Property(transaction => transaction.Description).IsRequired().HasMaxLength(2000);
            entity.HasIndex(transaction =>
                    new { transaction.StatementId, transaction.Sequence })
                .IsUnique();
            entity.HasOne(transaction => transaction.Statement)
                .WithMany(statement => statement.Transactions)
                .HasForeignKey(transaction => transaction.StatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StatementProcessingAudit>(entity =>
        {
            entity.ToTable("processing_audits");
            entity.Property(audit => audit.StatementType).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.AiProvider).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.AiModel).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.Status).IsRequired().HasMaxLength(50);
            entity.Property(audit => audit.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(audit => audit.StartedAtUtc);
            entity.HasOne(audit => audit.Statement)
                .WithMany(statement => statement.ProcessingAudits)
                .HasForeignKey(audit => audit.StatementId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        return Regex.Replace(
                Regex.Replace(value, "([A-Z]+)([A-Z][a-z])", "$1_$2"),
                "([a-z0-9])([A-Z])",
                "$1_$2")
            .ToLowerInvariant();
    }
}
