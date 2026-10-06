using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Statements.Definitions.CapitalOneBjs;

public sealed class CapitalOneBjsStatementDefinition : IStatementDefinition
{
    private const string PrivacyTokenInstruction =
        " Values beginning with 'CL_' and ending with a numeric identifier are privacy " +
        "tokens. Copy these values exactly into the matching response field. Do not alter, " +
        "expand, interpret, or omit them.";

    public StatementDefinitionDescriptor Descriptor { get; } = new(
        StatementDefinitionKeys.CapitalOneBjsOneMastercard,
        "Capital One",
        "BJ's One Mastercard",
        StatementKind.CreditCard,
        ["BJ's", "BJs", "BJ's One", "BJ's Capital One", "BJs Capital One"],
        ["BJ's One Mastercard", "BJs.capitalone.com"]);

    public string SummaryPrompt { get; } =
        "Read only the statement-level summary from this Capital One BJ's One Mastercard " +
        "statement. Return 'Capital One' as the issuer and copy the branded account name, " +
        "complete billing-cycle start and end dates, previous balance, new balance, and the " +
        "printed Account Summary totals. Read total_payments from the 'Payments' row and " +
        "total_other_credits from the 'Other Credits' row. Return fees from 'Fees Charged' " +
        "and interest from 'Interest Charged'. Return payment, other-credit, fee, and interest " +
        "totals as positive absolute values. Previous and new balances may be negative only " +
        "when the statement explicitly shows a credit balance. Purchases will be calculated " +
        "from individual transaction rows. Use ISO YYYY-MM-DD dates. Do not infer summary " +
        "amounts from transaction rows." +
        PrivacyTokenInstruction;

    public string TransactionPrompt { get; } =
        "Extract every ledger row visible on this single Capital One BJ's One Mastercard " +
        "statement page. Include every cardholder and account section. Read purchases from " +
        "'Transactions' and 'Transactions (Continued)' tables, and read payments, refunds, " +
        "returns, and other credits from 'Payments, Credits and Adjustments'. Use 'Trans Date' " +
        "as the transaction date; do not substitute 'Post Date'. Never summarize, sample, " +
        "combine, or omit rows. Do not turn headings, rewards, balances, promotional text, or " +
        "totals such as 'Total Transactions' and 'Total Transactions for This Period' into " +
        "transactions. For each row, use only the amount aligned with that row. Return amount " +
        "as a positive absolute value. Set is_credit to true for payments, refunds, returns, " +
        "statement credits, and adjustments that reduce the balance; otherwise set it to false. " +
        "Resolve the year from the supplied statement period and return ISO YYYY-MM-DD dates. " +
        "Return an empty array if the page contains no ledger rows." +
        PrivacyTokenInstruction;

    public object SummarySchema { get; } = CreateSummarySchema();

    public object TransactionSchema { get; } = CreateTransactionSchema();

    private static object CreateSummarySchema() => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            statement_summary = new
            {
                type = "object",
                additionalProperties = false,
                properties = new
                {
                    issuer = new { type = "string" },
                    account_name = new { type = "string" },
                    statement_period_start = new { type = "string" },
                    statement_period_end = new { type = "string" },
                    previous_balance = new { type = "number" },
                    new_balance = new { type = "number" },
                    total_payments = new { type = "number" },
                    total_other_credits = new { type = "number" },
                    fees = new { type = "number" },
                    interest_charged = new { type = "number" }
                },
                required = (string[])
                [
                    "issuer", "account_name", "statement_period_start", "statement_period_end",
                    "previous_balance", "new_balance", "total_payments", "total_other_credits",
                    "fees", "interest_charged"
                ]
            }
        },
        required = (string[])["statement_summary"]
    };

    private static object CreateTransactionSchema() => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            transactions = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        date = new { type = (string[])["string", "null"] },
                        category = new { type = "string" },
                        merchant = new { type = "string" },
                        description = new { type = "string" },
                        amount = new { type = "number" },
                        is_credit = new { type = "boolean" }
                    },
                    required = (string[])
                    [
                        "date", "category", "merchant", "description", "amount", "is_credit"
                    ]
                }
            }
        },
        required = (string[])["transactions"]
    };
}
