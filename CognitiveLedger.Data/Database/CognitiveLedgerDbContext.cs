using System;
using CognitiveLedger.Data.Models.CreditCard;
using CognitiveLedger.Data.Models;
using CognitiveLedger.Common.Types;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CognitiveLedger.Data.Database;

public class CognitiveLedgerDbContext : DbContext
{
    public CognitiveLedgerDbContext(DbContextOptions<CognitiveLedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CreditCardStatement> Statements => Set<CreditCardStatement>();
    public DbSet<CreditCardTransaction> Transactions => Set<CreditCardTransaction>();
    public DbSet<StatementProcessingAudit> ProcessingAudits => Set<StatementProcessingAudit>();
    public DbSet<StatementType> StatementTypes => Set<StatementType>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<AiProvider> AiProviders => Set<AiProvider>();
    public DbSet<AiModel> AiModels => Set<AiModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user_account");
            entity.Property(user => user.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(user => user.LastName).IsRequired().HasMaxLength(100);
            entity.Property(user => user.Address).HasMaxLength(1000);
            entity.Property(user => user.EmailAddress).IsRequired().HasMaxLength(320);
            entity.HasIndex(user => user.EmailAddress).IsUnique();
            entity.HasData(new
            {
                Id = UserCatalog.SystemUserId,
                FirstName = "system",
                LastName = "user",
                EmailAddress = "system@cognitiveledger.invalid",
                CreatedAtUtc = new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "system",
                IsActive = true,
                IsDeleted = false
            });
        });

        modelBuilder.Entity<CreditCardStatement>(entity =>
        {
            entity.ToTable("statement");
            entity.HasOne(statement => statement.User)
                .WithMany(user => user.Statements)
                .HasForeignKey(statement => statement.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(statement => statement.StatementType)
                .WithMany()
                .HasForeignKey(statement => statement.StatementTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(statement => statement.SourceDocumentSha256)
                .HasColumnName("source_document_sha256")
                .HasMaxLength(64);
            entity.HasIndex(statement =>
                    new { statement.UserId, statement.SourceDocumentSha256 })
                .IsUnique()
                .HasFilter("source_document_sha256 IS NOT NULL");
            entity.Property(statement => statement.Issuer).IsRequired().HasMaxLength(200);
            entity.Property(statement => statement.AccountName).IsRequired().HasMaxLength(300);
            entity.HasIndex(statement => new
            {
                statement.UserId,
                statement.Issuer,
                statement.AccountName,
                statement.StatementPeriodEnd
            });
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
            entity.Property(audit => audit.Filename).HasMaxLength(255);
            entity.Property(audit => audit.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(audit => new { audit.UserId, audit.StartedAtUtc });
            entity.HasOne(audit => audit.User)
                .WithMany(user => user.ProcessingAudits)
                .HasForeignKey(audit => audit.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(audit => audit.Statement)
                .WithMany()
                .HasForeignKey(audit => audit.StatementId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(audit => audit.Status)
                .WithMany()
                .HasForeignKey(audit => audit.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(audit => audit.StatementType)
                .WithMany()
                .HasForeignKey(audit => audit.StatementTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(audit => audit.AiProvider)
                .WithMany()
                .HasForeignKey(audit => audit.AiProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(audit => audit.AiModel)
                .WithMany()
                .HasForeignKey(audit => audit.AiModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiProvider>(entity =>
        {
            entity.ToTable("ai_provider");
            entity.HasKey(provider => provider.Id);
            entity.Property(provider => provider.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(provider => provider.Name).IsUnique();
            entity.HasData(new { Id = AiProviderCatalog.OpenAiId, Name = "OpenAI" });
        });

        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.ToTable("ai_model");
            entity.HasKey(model => model.Id);
            entity.Property(model => model.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(model => new { model.AiProviderId, model.Name }).IsUnique();
            entity.HasOne(model => model.AiProvider)
                .WithMany()
                .HasForeignKey(model => model.AiProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new
            {
                Id = AiModelCatalog.Gpt56TerraId,
                AiProviderId = AiProviderCatalog.OpenAiId,
                Name = "gpt-5.6-terra"
            });
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("status");
            entity.HasKey(status => status.Id);
            entity.Property(status => status.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(status => status.Name).IsUnique();
            entity.HasData(
                new { Id = StatusCatalog.ProcessingId, Name = "Processing" },
                new { Id = StatusCatalog.SuccessId, Name = "Success" },
                new { Id = StatusCatalog.FailedId, Name = "Failed" });
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
