using CognitiveLedger.Statements.Abstractions;

namespace CognitiveLedger.Statements.Definitions.SynchronyAmazon;

public sealed class SynchronyAmazonStatementDefinition : IStatementDefinition
{
    private const string PrivacyTokenInstruction =
        " Values beginning with 'CL_' and ending with a numeric identifier are privacy " +
        "tokens. Copy these values exactly into the matching response field. Do not alter, " +
        "expand, interpret, or omit them.";

    public StatementDefinitionDescriptor Descriptor { get; } = new(
        StatementDefinitionKeys.SynchronyAmazonStoreCard,
        "Synchrony Bank",
        "Amazon Store Card",
        StatementKind.CreditCard,
        ["Synchrony", "Amazon", "Amazon Store Card", "Synchrony Amazon"],
        ["Amazon Store Card", "amazon.syf.com"]);

    public string SummaryPrompt { get; } =
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
        "YYYY-MM-DD dates. Do not infer dates or calculate summary values from transaction rows." +
        PrivacyTokenInstruction;

    public string TransactionPrompt { get; } =
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
        "transaction rows. A later page may continue the transaction table without repeating " +
        "the section or column headings; extract every visible ledger row on such continuation " +
        "pages and do not return an empty array merely because headings are absent. Verify each " +
        "amount digit by digit, especially small statement credits." +
        PrivacyTokenInstruction;

    public object SummarySchema { get; } = new
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
                    "previous_balance", "new_balance",
                    "total_payments", "total_other_credits", "fees", "interest_charged"
                ]
            }
        },
        required = (string[])["statement_summary"]
    };

    public object TransactionSchema { get; } = new
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
