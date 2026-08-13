using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "CreditCardAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IssuerName = table.Column<string>(type: "TEXT", nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", nullable: true),
                    LastFourDigits = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardStatements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodEndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PaymentDueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PreviousBalance = table.Column<decimal>(type: "TEXT", nullable: true),
                    PaymentsAndCredits = table.Column<decimal>(type: "TEXT", nullable: true),
                    PurchasesTotal = table.Column<decimal>(type: "TEXT", nullable: true),
                    FeesTotal = table.Column<decimal>(type: "TEXT", nullable: true),
                    InterestTotal = table.Column<decimal>(type: "TEXT", nullable: true),
                    NewBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinimumPaymentDue = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "TEXT", nullable: true),
                    AvailableCredit = table.Column<decimal>(type: "TEXT", nullable: true),
                    SourceFileName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFileHash = table.Column<string>(type: "TEXT", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImportStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardStatements_CreditCardAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "CreditCardAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatementId = table.Column<long>(type: "INTEGER", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PostedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RawDescription = table.Column<string>(type: "TEXT", nullable: false),
                    MerchantName = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    OriginalCurrencyCode = table.Column<string>(type: "TEXT", nullable: true),
                    SourcePageNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    AiConfidence = table.Column<decimal>(type: "TEXT", nullable: true),
                    ReviewStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UserNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardTransactions_CreditCardStatements_StatementId",
                        column: x => x.StatementId,
                        principalTable: "CreditCardStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatements_AccountId",
                table: "CreditCardStatements",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardStatements_SourceFileHash",
                table: "CreditCardStatements",
                column: "SourceFileHash");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransactions_StatementId",
                table: "CreditCardTransactions",
                column: "StatementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardTransactions");

            migrationBuilder.DropTable(
                name: "CreditCardStatements");

            migrationBuilder.DropTable(
                name: "CreditCardAccounts");
        }
    }
}
