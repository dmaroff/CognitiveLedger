using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Statements.Definitions.CapitalOneDiscover;

public sealed class CapitalOneDiscoverStatementDefinition : IStatementDefinition
{
    private const string PrivacyTokenInstruction =
        " Values beginning with 'CL_' and ending with a numeric identifier are privacy " +
        "tokens. Copy these values exactly into the matching response field. Do not alter, " +
        "expand, interpret, or omit them.";

    public StatementDefinitionDescriptor Descriptor { get; } = new(
        StatementDefinitionKeys.CapitalOneDiscoverMore,
        "Capital One",
        "Discover More Card",
        StatementKind.CreditCard,
        ["Discover", "Discover More", "Discover by Capital One", "Capital One Discover"],
        ["Discover More Card", "discover.capitalone.com"]);

    public string SummaryPrompt { get; } =
        "Read only the statement-level summary from this Discover More Card by Capital One " +
        "statement. Return 'Capital One' as the issuer and copy the branded account name, the " +
        "complete OPEN TO CLOSE statement period, previous balance, new balance, and printed " +
        "Account Summary totals. The statement combines payments and all credits in one " +
        "'Payments and Credits' row: return that entire printed amount as total_payments and " +
        "return 0 as total_other_credits so the combined amount is counted exactly once. Return " +
        "fees from 'Fees Charged' and interest from 'Interest Charged'. Return totals as positive " +
        "absolute values. Previous and new balances may be negative only when the statement " +
        "explicitly shows a credit balance. Purchases will be calculated from individual " +
        "transaction rows. Use ISO YYYY-MM-DD dates. Do not infer summary amounts from " +
        "transaction rows." +
        PrivacyTokenInstruction;

    public string TransactionPrompt { get; } =
        "Extract every ledger row visible on this single Discover More Card by Capital One " +
        "statement page. Include rows from every cardholder and account section under " +
        "'Payments and Credits', 'Purchases', and any continuation tables. Use 'TRANS. DATE' " +
        "as the transaction date. Never summarize, sample, combine, or omit rows. Do not turn " +
        "headings, rewards, balances, promotional text, subtotals, or page totals into " +
        "transactions. For each row, use only the amount aligned with that row. Return amount " +
        "as a positive absolute value. Set is_credit to true for payments, refunds, returns, " +
        "statement credits, and other rows that reduce the balance; otherwise set it to false. " +
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
