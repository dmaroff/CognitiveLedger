using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "status_id",
                table: "processing_audit",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "status",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1L, "Processing" },
                    { 2L, "Success" },
                    { 3L, "Failed" }
                });

            migrationBuilder.Sql(
                """
                UPDATE processing_audit
                SET status_id = CASE status
                    WHEN 'Processing' THEN 1
                    WHEN 'Success' THEN 2
                    WHEN 'Succeeded' THEN 2
                    WHEN 'Failed' THEN 3
                END;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM processing_audit WHERE status_id IS NULL) THEN
                        RAISE EXCEPTION 'Cannot migrate an unknown processing_audit status.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "status_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "status",
                table: "processing_audit");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_status_id",
                table: "processing_audit",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_status_name",
                table: "status",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_status_status_id",
                table: "processing_audit",
                column: "status_id",
                principalTable: "status",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "processing_audit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE processing_audit AS audit
                SET status = status_lookup.name
                FROM status AS status_lookup
                WHERE audit.status_id = status_lookup.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "processing_audit",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_status_status_id",
                table: "processing_audit");

            migrationBuilder.DropTable(
                name: "status");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_status_id",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "status_id",
                table: "processing_audit");

        }
    }
}
