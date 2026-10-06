using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations;

public partial class UseIntegerAuditUserIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE user_account
            SET created_by = '1'
            WHERE id = 1 AND created_by = 'system';

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT created_by FROM user_account WHERE created_by IS NULL OR created_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT created_by FROM user_redaction_value WHERE created_by IS NULL OR created_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT created_by FROM statement WHERE created_by IS NULL OR created_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT created_by FROM "transaction" WHERE created_by IS NULL OR created_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT created_by FROM processing_audit WHERE created_by IS NULL OR created_by !~ '^[0-9]+$')
                THEN
                    RAISE EXCEPTION 'Populate every created_by value with a numeric user ID before applying this migration.';
                END IF;

                IF EXISTS (
                    SELECT updated_by FROM user_account WHERE updated_by IS NOT NULL AND updated_by <> '' AND updated_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT updated_by FROM user_redaction_value WHERE updated_by IS NOT NULL AND updated_by <> '' AND updated_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT updated_by FROM statement WHERE updated_by IS NOT NULL AND updated_by <> '' AND updated_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT updated_by FROM "transaction" WHERE updated_by IS NOT NULL AND updated_by <> '' AND updated_by !~ '^[0-9]+$'
                    UNION ALL
                    SELECT updated_by FROM processing_audit WHERE updated_by IS NOT NULL AND updated_by <> '' AND updated_by !~ '^[0-9]+$')
                THEN
                    RAISE EXCEPTION 'Every populated updated_by value must be a numeric user ID before applying this migration.';
                END IF;
            END $$;

            ALTER TABLE user_account
                ALTER COLUMN created_by TYPE integer USING created_by::integer,
                ALTER COLUMN created_by SET NOT NULL,
                ALTER COLUMN updated_by TYPE integer USING NULLIF(updated_by, '')::integer;

            ALTER TABLE user_redaction_value
                ALTER COLUMN created_by TYPE integer USING created_by::integer,
                ALTER COLUMN created_by SET NOT NULL,
                ALTER COLUMN updated_by TYPE integer USING NULLIF(updated_by, '')::integer;

            ALTER TABLE statement
                ALTER COLUMN created_by TYPE integer USING created_by::integer,
                ALTER COLUMN created_by SET NOT NULL,
                ALTER COLUMN updated_by TYPE integer USING NULLIF(updated_by, '')::integer;

            ALTER TABLE "transaction"
                ALTER COLUMN created_by TYPE integer USING created_by::integer,
                ALTER COLUMN created_by SET NOT NULL,
                ALTER COLUMN updated_by TYPE integer USING NULLIF(updated_by, '')::integer;

            ALTER TABLE processing_audit
                ALTER COLUMN created_by TYPE integer USING created_by::integer,
                ALTER COLUMN created_by SET NOT NULL,
                ALTER COLUMN updated_by TYPE integer USING NULLIF(updated_by, '')::integer;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE user_account
                ALTER COLUMN created_by DROP NOT NULL,
                ALTER COLUMN created_by TYPE text USING created_by::text,
                ALTER COLUMN updated_by TYPE text USING updated_by::text;

            ALTER TABLE user_redaction_value
                ALTER COLUMN created_by DROP NOT NULL,
                ALTER COLUMN created_by TYPE text USING created_by::text,
                ALTER COLUMN updated_by TYPE text USING updated_by::text;

            ALTER TABLE statement
                ALTER COLUMN created_by DROP NOT NULL,
                ALTER COLUMN created_by TYPE text USING created_by::text,
                ALTER COLUMN updated_by TYPE text USING updated_by::text;

            ALTER TABLE "transaction"
                ALTER COLUMN created_by DROP NOT NULL,
                ALTER COLUMN created_by TYPE text USING created_by::text,
                ALTER COLUMN updated_by TYPE text USING updated_by::text;

            ALTER TABLE processing_audit
                ALTER COLUMN created_by DROP NOT NULL,
                ALTER COLUMN created_by TYPE text USING created_by::text,
                ALTER COLUMN updated_by TYPE text USING updated_by::text;
            """);
    }
}
