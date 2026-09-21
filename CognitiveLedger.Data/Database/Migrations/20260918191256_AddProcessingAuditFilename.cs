using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingAuditFilename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "filename",
                table: "processing_audit",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "filename",
                table: "processing_audit");
        }
    }
}
