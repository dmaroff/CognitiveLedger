using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "statements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StatementPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    StatementPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    NewBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPurchases = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPayments = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalOtherCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    Fees = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestCharged = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processing_audits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatementId = table.Column<long>(type: "bigint", nullable: true),
                    StatementType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AiProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AiModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    ExtractedTransactionCount = table.Column<int>(type: "integer", nullable: true),
                    IgnoredRowCount = table.Column<int>(type: "integer", nullable: false),
                    CorrectedRowCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_processing_audits_statements_StatementId",
                        column: x => x.StatementId,
                        principalTable: "statements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatementId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Merchant = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsCredit = table.Column<bool>(type: "boolean", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_statements_StatementId",
                        column: x => x.StatementId,
                        principalTable: "statements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_processing_audits_StartedAtUtc",
                table: "processing_audits",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audits_StatementId",
                table: "processing_audits",
                column: "StatementId");

            migrationBuilder.CreateIndex(
                name: "IX_statements_Issuer_AccountName_StatementPeriodEnd",
                table: "statements",
                columns: new[] { "Issuer", "AccountName", "StatementPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_StatementId_Sequence",
                table: "transactions",
                columns: new[] { "StatementId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processing_audits");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "statements");
        }
    }
}
