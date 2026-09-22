using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_statement_issuer_account_name_statement_period_end",
                table: "statement");

            migrationBuilder.DropIndex(
                name: "IX_statement_source_document_sha256",
                table: "statement");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_started_at_utc",
                table: "processing_audit");

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "statement",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "user_account",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    email_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_account", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "user_account",
                columns: new[] { "id", "address", "created_at_utc", "created_by", "date_of_birth", "email_address", "first_name", "is_active", "is_deleted", "last_name", "updated_at_utc", "updated_by" },
                values: new object[] { 1L, null, new DateTime(2026, 9, 22, 0, 0, 0, 0, DateTimeKind.Utc), "system", null, "system@cognitiveledger.invalid", "system", true, false, "user", null, null });

            migrationBuilder.Sql("UPDATE statement SET user_id = 1 WHERE user_id = 0;");
            migrationBuilder.Sql("UPDATE processing_audit SET user_id = 1 WHERE user_id = 0;");

            // Remove the temporary migration defaults. New records must always
            // provide their owner explicitly.
            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "statement",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldDefaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_statement_user_id_issuer_account_name_statement_period_end",
                table: "statement",
                columns: new[] { "user_id", "issuer", "account_name", "statement_period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_statement_user_id_source_document_sha256",
                table: "statement",
                columns: new[] { "user_id", "source_document_sha256" },
                unique: true,
                filter: "source_document_sha256 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_user_id_started_at_utc",
                table: "processing_audit",
                columns: new[] { "user_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_account_email_address",
                table: "user_account",
                column: "email_address",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_user_account_user_id",
                table: "processing_audit",
                column: "user_id",
                principalTable: "user_account",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_statement_user_account_user_id",
                table: "statement",
                column: "user_id",
                principalTable: "user_account",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_user_account_user_id",
                table: "processing_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_user_account_user_id",
                table: "statement");

            migrationBuilder.DropTable(
                name: "user_account");

            migrationBuilder.DropIndex(
                name: "IX_statement_user_id_issuer_account_name_statement_period_end",
                table: "statement");

            migrationBuilder.DropIndex(
                name: "IX_statement_user_id_source_document_sha256",
                table: "statement");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_user_id_started_at_utc",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "statement");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "processing_audit");

            migrationBuilder.CreateIndex(
                name: "IX_statement_issuer_account_name_statement_period_end",
                table: "statement",
                columns: new[] { "issuer", "account_name", "statement_period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_statement_source_document_sha256",
                table: "statement",
                column: "source_document_sha256",
                unique: true,
                filter: "source_document_sha256 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_started_at_utc",
                table: "processing_audit",
                column: "started_at_utc");
        }
    }
}
