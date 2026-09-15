using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementTypeAndSingularTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processing_audits_statements_statement_id",
                table: "processing_audits");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_statements_statement_id",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statements",
                table: "statements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_processing_audits",
                table: "processing_audits");

            migrationBuilder.RenameTable(
                name: "transactions",
                newName: "transaction");

            migrationBuilder.RenameTable(
                name: "statements",
                newName: "statement");

            migrationBuilder.RenameTable(
                name: "processing_audits",
                newName: "processing_audit");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_statement_id_sequence",
                table: "transaction",
                newName: "IX_transaction_statement_id_sequence");

            migrationBuilder.RenameIndex(
                name: "IX_statements_source_document_sha256",
                table: "statement",
                newName: "IX_statement_source_document_sha256");

            migrationBuilder.RenameIndex(
                name: "IX_statements_issuer_account_name_statement_period_end",
                table: "statement",
                newName: "IX_statement_issuer_account_name_statement_period_end");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_statement_id",
                table: "processing_audit",
                newName: "IX_processing_audit_statement_id");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_started_at_utc",
                table: "processing_audit",
                newName: "IX_processing_audit_started_at_utc");

            migrationBuilder.AddColumn<long>(
                name: "statement_type_id",
                table: "statement",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_transaction",
                table: "transaction",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_statement",
                table: "statement",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_processing_audit",
                table: "processing_audit",
                column: "id");

            migrationBuilder.CreateTable(
                name: "statement_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_type", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "statement_type",
                columns: new[] { "id", "code", "description", "name" },
                values: new object[,]
                {
                    { 1L, "UNKNOWN", "The statement type has not been identified.", "Unknown" },
                    { 2L, "BKACT", "A bank account statement.", "BankAccount" },
                    { 3L, "CRDCRD", "A credit card statement.", "CreditCard" },
                    { 4L, "RTLACC", "A retail or store account statement.", "RetailAccount" },
                    { 5L, "MEDBIL", "A bill or account statement from a healthcare provider.", "MedicalBill" },
                    { 6L, "UTLBIL", "A bill or account statement from a utility provider.", "UtilityBill" },
                    { 7L, "OTHER", "A statement that does not match another supported type.", "Other" }
                });

            // Existing rows are credit-card statements. Backfill before enforcing the FK.
            migrationBuilder.Sql("UPDATE statement SET statement_type_id = 3 WHERE statement_type_id IS NULL;");

            migrationBuilder.AlterColumn<long>(
                name: "statement_type_id",
                table: "statement",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_statement_statement_type_id",
                table: "statement",
                column: "statement_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_statement_type_code",
                table: "statement_type",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_statement_statement_id",
                table: "processing_audit",
                column: "statement_id",
                principalTable: "statement",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_statement_statement_type_statement_type_id",
                table: "statement",
                column: "statement_type_id",
                principalTable: "statement_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transaction_statement_statement_id",
                table: "transaction",
                column: "statement_id",
                principalTable: "statement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_statement_statement_id",
                table: "processing_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_statement_type_statement_type_id",
                table: "statement");

            migrationBuilder.DropForeignKey(
                name: "FK_transaction_statement_statement_id",
                table: "transaction");

            migrationBuilder.DropTable(
                name: "statement_type");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transaction",
                table: "transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statement",
                table: "statement");

            migrationBuilder.DropIndex(
                name: "IX_statement_statement_type_id",
                table: "statement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_processing_audit",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "statement_type_id",
                table: "statement");

            migrationBuilder.RenameTable(
                name: "transaction",
                newName: "transactions");

            migrationBuilder.RenameTable(
                name: "statement",
                newName: "statements");

            migrationBuilder.RenameTable(
                name: "processing_audit",
                newName: "processing_audits");

            migrationBuilder.RenameIndex(
                name: "IX_transaction_statement_id_sequence",
                table: "transactions",
                newName: "IX_transactions_statement_id_sequence");

            migrationBuilder.RenameIndex(
                name: "IX_statement_source_document_sha256",
                table: "statements",
                newName: "IX_statements_source_document_sha256");

            migrationBuilder.RenameIndex(
                name: "IX_statement_issuer_account_name_statement_period_end",
                table: "statements",
                newName: "IX_statements_issuer_account_name_statement_period_end");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audit_statement_id",
                table: "processing_audits",
                newName: "IX_processing_audits_statement_id");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audit_started_at_utc",
                table: "processing_audits",
                newName: "IX_processing_audits_started_at_utc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_statements",
                table: "statements",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_processing_audits",
                table: "processing_audits",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audits_statements_statement_id",
                table: "processing_audits",
                column: "statement_id",
                principalTable: "statements",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_statements_statement_id",
                table: "transactions",
                column: "statement_id",
                principalTable: "statements",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
