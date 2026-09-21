using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeProcessingAuditMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ai_model_id",
                table: "processing_audit",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ai_provider_id",
                table: "processing_audit",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "statement_type_id",
                table: "processing_audit",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_provider",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_provider", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_model",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ai_provider_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_model", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_model_ai_provider_ai_provider_id",
                        column: x => x.ai_provider_id,
                        principalTable: "ai_provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ai_provider",
                columns: new[] { "id", "name" },
                values: new object[] { 1L, "OpenAI" });

            migrationBuilder.InsertData(
                table: "ai_model",
                columns: new[] { "id", "ai_provider_id", "name" },
                values: new object[] { 1L, 1L, "gpt-5.6-terra" });

            migrationBuilder.Sql(
                """
                UPDATE processing_audit AS audit
                SET ai_provider_id = provider.id
                FROM ai_provider AS provider
                WHERE audit.ai_provider = provider.name;

                UPDATE processing_audit AS audit
                SET ai_model_id = model.id
                FROM ai_model AS model
                INNER JOIN ai_provider AS provider ON provider.id = model.ai_provider_id
                WHERE audit.ai_model = model.name
                  AND audit.ai_provider = provider.name;

                UPDATE processing_audit AS audit
                SET statement_type_id = statement_type.id
                FROM statement_type
                WHERE audit.statement_type = statement_type.name
                   OR audit.statement_type = statement_type.code;

                UPDATE processing_audit
                SET statement_type_id = 3
                WHERE statement_type = 'SynchronyAmazon'
                  AND statement_type_id IS NULL;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM processing_audit
                        WHERE ai_provider_id IS NULL
                           OR ai_model_id IS NULL
                           OR statement_type_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate unknown processing audit metadata.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "ai_model_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ai_provider_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "statement_type_id",
                table: "processing_audit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "ai_model", table: "processing_audit");
            migrationBuilder.DropColumn(name: "ai_provider", table: "processing_audit");
            migrationBuilder.DropColumn(name: "statement_type", table: "processing_audit");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_ai_model_id",
                table: "processing_audit",
                column: "ai_model_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_ai_provider_id",
                table: "processing_audit",
                column: "ai_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_audit_statement_type_id",
                table: "processing_audit",
                column: "statement_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_model_ai_provider_id_name",
                table: "ai_model",
                columns: new[] { "ai_provider_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_provider_name",
                table: "ai_provider",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_ai_model_ai_model_id",
                table: "processing_audit",
                column: "ai_model_id",
                principalTable: "ai_model",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_ai_provider_ai_provider_id",
                table: "processing_audit",
                column: "ai_provider_id",
                principalTable: "ai_provider",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audit_statement_type_statement_type_id",
                table: "processing_audit",
                column: "statement_type_id",
                principalTable: "statement_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_model",
                table: "processing_audit",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ai_provider",
                table: "processing_audit",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "statement_type",
                table: "processing_audit",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE processing_audit AS audit
                SET ai_model = model.name
                FROM ai_model AS model
                WHERE audit.ai_model_id = model.id;

                UPDATE processing_audit AS audit
                SET ai_provider = provider.name
                FROM ai_provider AS provider
                WHERE audit.ai_provider_id = provider.id;

                UPDATE processing_audit AS audit
                SET statement_type = statement_type.name
                FROM statement_type
                WHERE audit.statement_type_id = statement_type.id;
                """);

            migrationBuilder.AlterColumn<string>(name: "ai_model", table: "processing_audit", type: "character varying(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ai_provider", table: "processing_audit", type: "character varying(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "statement_type", table: "processing_audit", type: "character varying(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "character varying(100)", oldMaxLength: 100, oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_ai_model_ai_model_id",
                table: "processing_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_ai_provider_ai_provider_id",
                table: "processing_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_processing_audit_statement_type_statement_type_id",
                table: "processing_audit");

            migrationBuilder.DropTable(
                name: "ai_model");

            migrationBuilder.DropTable(
                name: "ai_provider");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_ai_model_id",
                table: "processing_audit");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_ai_provider_id",
                table: "processing_audit");

            migrationBuilder.DropIndex(
                name: "IX_processing_audit_statement_type_id",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "ai_model_id",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "ai_provider_id",
                table: "processing_audit");

            migrationBuilder.DropColumn(
                name: "statement_type_id",
                table: "processing_audit");

        }
    }
}
