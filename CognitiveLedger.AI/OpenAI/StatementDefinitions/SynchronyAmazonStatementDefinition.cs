namespace CognitiveLedger.AI.OpenAI.StatementDefinitions;

public static class SynchronyAmazonStatementDefinition
{
    public const string SummaryPrompt =
        "Read only the statement-level summary from this Synchrony Amazon credit-card " +
        "statement. Copy the issuer, account name, complete statement period, previous " +
        "balance, new balance, and the printed totals for payments, other credits, " +
        "fees, and interest exactly as shown. Read total_payments from the bold amount directly " +
        "across from the 'Payments' section heading. Read total_other_credits from the bold " +
        "amount directly across from the 'Other Credits' section heading. Do not combine these " +
        "two values. Return purchase, payment, other-credit, fee, and interest totals as positive " +
        "absolute values. Previous and new balances may be " +
        "negative only when the statement explicitly shows a credit balance. Ignore the bold " +
        "'Purchases and Other Debits' section total; purchases will be calculated from the " +
        "individual transaction rows. Use ISO " +
        "YYYY-MM-DD dates. Do not infer dates or calculate summary values from transaction rows.";

    public const string TransactionPrompt =
        "Extract every credit-card transaction row visible on this single Synchrony Amazon " +
        "statement page. Never summarize, sample, combine, or omit rows. Include purchases, " +
        "payments, refunds, returns, statement credits, fees, and interest when they appear " +
        "as ledger rows. Do not turn statement-summary boxes, subtotals, balances, rewards, " +
        "or payment coupons into transactions. In particular, section headings such as " +
        "'Purchases and Other Debits', 'Payments', and 'Other Credits' with a subtotal amount " +
        "directly across from the heading are not transaction rows and must be ignored. The " +
        "'Purchases and Other Debits' amount is a section total that will be calculated and " +
        "validated separately; never return it as a transaction. For each ledger row, use only " +
        "the amount horizontally aligned with that row. Never copy a section subtotal onto the " +
        "first transaction beneath it. Return amount as a positive " +
        "absolute value. Set is_credit " +
        "to true for payments, refunds, returns, statement credits, and other rows that reduce " +
        "the balance; otherwise set it to false. Return a transaction date as a complete ISO " +
        "YYYY-MM-DD date only when it can be determined unambiguously from the page and supplied " +
        "statement period; otherwise return null. Return an empty array if this page has no " +
        "transaction rows.";

    public static object SummarySchema { get; } = new
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
                required = new[]
                {
                    "issuer", "account_name", "statement_period_start", "statement_period_end",
                    "previous_balance", "new_balance",
                    "total_payments", "total_other_credits", "fees", "interest_charged"
                }
            }
        },
        required = new[] { "statement_summary" }
    };

    public static object TransactionSchema { get; } = new
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
                        date = new { type = new[] { "string", "null" } },
                        category = new { type = "string" },
                        merchant = new { type = "string" },
                        description = new { type = "string" },
                        amount = new { type = "number" },
                        is_credit = new { type = "boolean" }
                    },
                    required = new[]
                    {
                        "date", "category", "merchant", "description", "amount", "is_credit"
                    }
                }
            }
        },
        required = new[] { "transactions" }
    };
}
