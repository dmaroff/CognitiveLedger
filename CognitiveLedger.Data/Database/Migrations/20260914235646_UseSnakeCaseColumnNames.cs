using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CognitiveLedger.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCaseColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processing_audits_statements_StatementId",
                table: "processing_audits");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_statements_StatementId",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "Sequence",
                table: "transactions",
                newName: "sequence");

            migrationBuilder.RenameColumn(
                name: "Merchant",
                table: "transactions",
                newName: "merchant");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "transactions",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "transactions",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "transactions",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "transactions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "transactions",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "transactions",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "transactions",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "StatementId",
                table: "transactions",
                newName: "statement_id");

            migrationBuilder.RenameColumn(
                name: "IsCredit",
                table: "transactions",
                newName: "is_credit");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "transactions",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "transactions",
                newName: "created_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_StatementId_Sequence",
                table: "transactions",
                newName: "IX_transactions_statement_id_sequence");

            migrationBuilder.RenameColumn(
                name: "Issuer",
                table: "statements",
                newName: "issuer");

            migrationBuilder.RenameColumn(
                name: "Fees",
                table: "statements",
                newName: "fees");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "statements",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "statements",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "statements",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "TotalPurchases",
                table: "statements",
                newName: "total_purchases");

            migrationBuilder.RenameColumn(
                name: "TotalPayments",
                table: "statements",
                newName: "total_payments");

            migrationBuilder.RenameColumn(
                name: "TotalOtherCredits",
                table: "statements",
                newName: "total_other_credits");

            migrationBuilder.RenameColumn(
                name: "StatementPeriodStart",
                table: "statements",
                newName: "statement_period_start");

            migrationBuilder.RenameColumn(
                name: "StatementPeriodEnd",
                table: "statements",
                newName: "statement_period_end");

            migrationBuilder.RenameColumn(
                name: "PreviousBalance",
                table: "statements",
                newName: "previous_balance");

            migrationBuilder.RenameColumn(
                name: "NewBalance",
                table: "statements",
                newName: "new_balance");

            migrationBuilder.RenameColumn(
                name: "InterestCharged",
                table: "statements",
                newName: "interest_charged");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "statements",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "statements",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "statements",
                newName: "account_name");

            migrationBuilder.RenameIndex(
                name: "IX_statements_Issuer_AccountName_StatementPeriodEnd",
                table: "statements",
                newName: "IX_statements_issuer_account_name_statement_period_end");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "processing_audits",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "processing_audits",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "processing_audits",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "processing_audits",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "StatementType",
                table: "processing_audits",
                newName: "statement_type");

            migrationBuilder.RenameColumn(
                name: "StatementId",
                table: "processing_audits",
                newName: "statement_id");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "processing_audits",
                newName: "started_at_utc");

            migrationBuilder.RenameColumn(
                name: "PageCount",
                table: "processing_audits",
                newName: "page_count");

            migrationBuilder.RenameColumn(
                name: "IgnoredRowCount",
                table: "processing_audits",
                newName: "ignored_row_count");

            migrationBuilder.RenameColumn(
                name: "ExtractedTransactionCount",
                table: "processing_audits",
                newName: "extracted_transaction_count");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "processing_audits",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "DurationMilliseconds",
                table: "processing_audits",
                newName: "duration_milliseconds");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "processing_audits",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "processing_audits",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "CorrectedRowCount",
                table: "processing_audits",
                newName: "corrected_row_count");

            migrationBuilder.RenameColumn(
                name: "CompletedAtUtc",
                table: "processing_audits",
                newName: "completed_at_utc");

            migrationBuilder.RenameColumn(
                name: "AiProvider",
                table: "processing_audits",
                newName: "ai_provider");

            migrationBuilder.RenameColumn(
                name: "AiModel",
                table: "processing_audits",
                newName: "ai_model");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_StatementId",
                table: "processing_audits",
                newName: "IX_processing_audits_statement_id");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_StartedAtUtc",
                table: "processing_audits",
                newName: "IX_processing_audits_started_at_utc");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processing_audits_statements_statement_id",
                table: "processing_audits");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_statements_statement_id",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "sequence",
                table: "transactions",
                newName: "Sequence");

            migrationBuilder.RenameColumn(
                name: "merchant",
                table: "transactions",
                newName: "Merchant");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "transactions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "transactions",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "transactions",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "transactions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "transactions",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "transactions",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "transactions",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "statement_id",
                table: "transactions",
                newName: "StatementId");

            migrationBuilder.RenameColumn(
                name: "is_credit",
                table: "transactions",
                newName: "IsCredit");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "transactions",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "transactions",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_statement_id_sequence",
                table: "transactions",
                newName: "IX_transactions_StatementId_Sequence");

            migrationBuilder.RenameColumn(
                name: "issuer",
                table: "statements",
                newName: "Issuer");

            migrationBuilder.RenameColumn(
                name: "fees",
                table: "statements",
                newName: "Fees");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "statements",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "statements",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "statements",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "total_purchases",
                table: "statements",
                newName: "TotalPurchases");

            migrationBuilder.RenameColumn(
                name: "total_payments",
                table: "statements",
                newName: "TotalPayments");

            migrationBuilder.RenameColumn(
                name: "total_other_credits",
                table: "statements",
                newName: "TotalOtherCredits");

            migrationBuilder.RenameColumn(
                name: "statement_period_start",
                table: "statements",
                newName: "StatementPeriodStart");

            migrationBuilder.RenameColumn(
                name: "statement_period_end",
                table: "statements",
                newName: "StatementPeriodEnd");

            migrationBuilder.RenameColumn(
                name: "previous_balance",
                table: "statements",
                newName: "PreviousBalance");

            migrationBuilder.RenameColumn(
                name: "new_balance",
                table: "statements",
                newName: "NewBalance");

            migrationBuilder.RenameColumn(
                name: "interest_charged",
                table: "statements",
                newName: "InterestCharged");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "statements",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "statements",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "account_name",
                table: "statements",
                newName: "AccountName");

            migrationBuilder.RenameIndex(
                name: "IX_statements_issuer_account_name_statement_period_end",
                table: "statements",
                newName: "IX_statements_Issuer_AccountName_StatementPeriodEnd");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "processing_audits",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "processing_audits",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "processing_audits",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "processing_audits",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "statement_type",
                table: "processing_audits",
                newName: "StatementType");

            migrationBuilder.RenameColumn(
                name: "statement_id",
                table: "processing_audits",
                newName: "StatementId");

            migrationBuilder.RenameColumn(
                name: "started_at_utc",
                table: "processing_audits",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "page_count",
                table: "processing_audits",
                newName: "PageCount");

            migrationBuilder.RenameColumn(
                name: "ignored_row_count",
                table: "processing_audits",
                newName: "IgnoredRowCount");

            migrationBuilder.RenameColumn(
                name: "extracted_transaction_count",
                table: "processing_audits",
                newName: "ExtractedTransactionCount");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "processing_audits",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "duration_milliseconds",
                table: "processing_audits",
                newName: "DurationMilliseconds");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "processing_audits",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "processing_audits",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "corrected_row_count",
                table: "processing_audits",
                newName: "CorrectedRowCount");

            migrationBuilder.RenameColumn(
                name: "completed_at_utc",
                table: "processing_audits",
                newName: "CompletedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ai_provider",
                table: "processing_audits",
                newName: "AiProvider");

            migrationBuilder.RenameColumn(
                name: "ai_model",
                table: "processing_audits",
                newName: "AiModel");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_statement_id",
                table: "processing_audits",
                newName: "IX_processing_audits_StatementId");

            migrationBuilder.RenameIndex(
                name: "IX_processing_audits_started_at_utc",
                table: "processing_audits",
                newName: "IX_processing_audits_StartedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_processing_audits_statements_StatementId",
                table: "processing_audits",
                column: "StatementId",
                principalTable: "statements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_statements_StatementId",
                table: "transactions",
                column: "StatementId",
                principalTable: "statements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
