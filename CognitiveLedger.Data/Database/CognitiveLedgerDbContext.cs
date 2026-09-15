using CognitiveLedger.Data.Models.CreditCard;
using CognitiveLedger.Data.Models;
using CognitiveLedger.Common.Types;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CognitiveLedger.Data.Database;

public class CognitiveLedgerDbContext(DbContextOptions<CognitiveLedgerDbContext> options)
    : DbContext(options)
{
    public DbSet<CreditCardStatement> Statements => Set<CreditCardStatement>();
    public DbSet<CreditCardTransaction> Transactions => Set<CreditCardTransaction>();
    public DbSet<StatementProcessingAudit> ProcessingAudits => Set<StatementProcessingAudit>();
    public DbSet<StatementType> StatementTypes => Set<StatementType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCardStatement>(entity =>
        {
            entity.ToTable("statement");
            entity.HasOne(statement => statement.StatementType)
                .WithMany(type => type.Statements)
                .HasForeignKey(statement => statement.StatementTypeId)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.ToTable("transaction");
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
            entity.ToTable("processing_audit");
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

        modelBuilder.Entity<StatementType>(entity =>
        {
            entity.ToTable("statement_type");
            entity.HasKey(type => type.Id);
            entity.Property(type => type.Code).IsRequired().HasMaxLength(20);
            entity.Property(type => type.Name).IsRequired().HasMaxLength(100);
            entity.Property(type => type.Description).IsRequired().HasMaxLength(500);
            entity.HasIndex(type => type.Code).IsUnique();
            entity.HasData(
                new { Id = StatementTypeCatalog.UnknownId, Code = StatementTypeCatalog.UnknownCode, Name = "Unknown", Description = "The statement type has not been identified." },
                new { Id = StatementTypeCatalog.BankAccountId, Code = StatementTypeCatalog.BankAccountCode, Name = "BankAccount", Description = "A bank account statement." },
                new { Id = StatementTypeCatalog.CreditCardId, Code = StatementTypeCatalog.CreditCardCode, Name = "CreditCard", Description = "A credit card statement." },
                new { Id = StatementTypeCatalog.RetailAccountId, Code = StatementTypeCatalog.RetailAccountCode, Name = "RetailAccount", Description = "A retail or store account statement." },
                new { Id = StatementTypeCatalog.MedicalBillId, Code = StatementTypeCatalog.MedicalBillCode, Name = "MedicalBill", Description = "A bill or account statement from a healthcare provider." },
                new { Id = StatementTypeCatalog.UtilityBillId, Code = StatementTypeCatalog.UtilityBillCode, Name = "UtilityBill", Description = "A bill or account statement from a utility provider." },
                new { Id = StatementTypeCatalog.OtherId, Code = StatementTypeCatalog.OtherCode, Name = "Other", Description = "A statement that does not match another supported type." });
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
