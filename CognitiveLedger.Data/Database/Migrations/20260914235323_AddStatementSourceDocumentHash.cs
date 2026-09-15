using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementSourceDocumentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source_document_sha256",
                table: "statements",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_statements_source_document_sha256",
                table: "statements",
                column: "source_document_sha256",
                unique: true,
                filter: "source_document_sha256 IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_statements_source_document_sha256",
                table: "statements");

            migrationBuilder.DropColumn(
                name: "source_document_sha256",
                table: "statements");
        }
    }
}
