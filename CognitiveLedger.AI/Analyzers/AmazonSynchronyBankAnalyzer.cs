using System.Text.Json;
using System.Text.RegularExpressions;
using CognitiveLedger.AI.AIPlatform;
using CognitiveLedger.AI.Analyzers.Prompts;
using CognitiveLedger.Common.Types;

namespace CognitiveLedger.AI.Analyzers;

public enum ResponseStatus
{
    Success,
    Failure,
    Timeout
}

public abstract class ResponseBase
{
    public required ResponseStatus Status { get; set; }
}

public class AnalyzeStatementResponse : ResponseBase
{
    public required CreditCardStatementDto CreditCardStatement { get; set; }
}


public class AmazonSynchronyBankAnalyzer : IStatementAnalyzer
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

    // The fallback's per-block calls are individually tiny (a few lines in, one small object
    // out), so each gets its own short budget rather than sharing _timeout across all of them.
    private readonly TimeSpan _blockTimeout = TimeSpan.FromSeconds(30);
    private readonly IAiPlatform _aiPlatform;

    // Marks the start of the statement's line-item section. Slicing the prompt down to just
    // this section (and everything after it) for the transactions call keeps the prompt small
    // enough to fit Ollama's default context window without needing a larger num_ctx - raising
    // num_ctx instead added enough per-token overhead to turn "fast but truncated" into
    // "cancels outright".
    private const string TransactionSectionMarker = "Transaction Detail";

    // Every field has a concrete type (string/number/object), so an empty "{}" can't
    // trivially satisfy a required field the way it could when fields were left unconstrained.
    // Deliberately avoids ["string", "null"]-style nullable unions repeated at every level -
    // that pattern, combined with nested "required", is what pushed Ollama's grammar-
    // constrained decoding into a pathologically slow path earlier. Optional fields (like
    // referenceNumber) are left out of "required" instead of union-typed with null.
    private static readonly object HeaderResponseFormat = new
    {
        type = "object",
        properties = new
        {
            cardHolderName = new { type = "string" },
            accountLastFourDigits = new { type = "string" },
            periodStartDate = new { type = "string" },
            periodEndDate = new { type = "string" },
            minimumPaymentDue = new
            {
                type = "object",
                properties = new
                {
                    dueDate = new { type = "string" },
                    amount = new { type = "number" }
                },
                required = new[] { "dueDate", "amount" }
            },
            totalPaymentDue = new
            {
                type = "object",
                properties = new
                {
                    dueDate = new { type = "string" },
                    amount = new { type = "number" }
                },
                required = new[] { "dueDate", "amount" }
            },
            purchasesAndOtherDebits = new { type = "number" }
        },
        required = new[]
        {
            "cardHolderName", "accountLastFourDigits", "periodStartDate", "periodEndDate",
            "minimumPaymentDue", "totalPaymentDue", "purchasesAndOtherDebits"
        }
    };

    // Detects the start of a transaction line: "MM/DD ... $amount". Deliberately just a
    // boundary check - AmountRegex below is what actually extracts the amount and its sign,
    // since folding that into this same greedy pattern let "rest" silently swallow a leading
    // "-" before the sign group ever got a chance to claim it (backtracking stops at the first
    // successful match, and "rest capturing the dash" succeeds before "sign capturing the dash"
    // is ever tried) - misclassifying every credit/payment/refund as a positive "Purchase".
    private static readonly Regex TransactionLineRegex = new(
        @"^(?<month>\d{2})/(?<day>\d{2})\s+.*\$[\d,]+\.\d{2}\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Finds every "-$amount" / "$amount" occurrence on a line; the transaction's real amount is
    // always the LAST one, even when an earlier one is noise glued in by this statement's PDF
    // text extraction interleaving an unrelated column into the line.
    private static readonly Regex AmountRegex = new(
        @"(?<sign>-)?\$(?<amount>[\d,]+\.\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex ReferenceNumberRegex = new(
        @"^(?<ref>P9[0-9A-Z]{15})",
        RegexOptions.Compiled);

    // The item name (when present) sits a line or two below the transaction row, past a short
    // random-looking string (e.g. "CIctxoiFSDJr") that the PDF's text extraction picks up as if
    // it were real content - likely some kind of embedded watermark/tracking text rather than
    // anything printed for a reader. Letters-only, no spaces or digits, distinguishes it from an
    // actual item name.
    private static readonly Regex NoiseLineRegex = new(
        @"^[A-Za-z]{10,20}$",
        RegexOptions.Compiled);

    // The statement's "Account Summary" sidebar (Credit Limit, Previous Balance, etc.) can end
    // up interleaved between a transaction row and its real item name by this statement's PDF
    // text extraction. These labels are a small, fixed, known set - never a real item name -
    // so excluding them by prefix stops them from being mistaken for one, even on the messiest
    // blocks where the actual item name isn't recoverable within the lookahead window at all.
    private static readonly string[] AccountSummaryBoilerplatePrefixes =
    [
        "Credit Limit", "Previous Balance", "Payments", "Available Credit", "Other Credits",
        "Purchases/Debits", "Rewards Summary", "Rewards Earned", "New Balance",
        "Total Minimum Payment Due", "Payment Due Date", "See Rewards Detail",
        "Day Billing Cycle", "Continued on next page", "PAGE ", "Visit us at", "Transaction Detail",
        "Date Reference #"
    ];

    // Only these exact generic merchant/location strings are worth replacing with a real item
    // name - anything else on a transaction line (a payment, a statement credit, an interest
    // charge) is already a complete, correct, standalone description on its own and shouldn't
    // have a nearby unrelated line substituted in for it.
    private static readonly Regex MerchantBoilerplateRegex = new(
        @"^(AMAZON (RETAIL|MARKETPLACE)|PRIME BILLING|DIGITAL PURCHASE)\s+SEATTLE\s+WA$",
        RegexOptions.Compiled);

    // One call per transaction gets one of these - a single object, not an array - so the
    // fallback's schema stays this small regardless of how many transactions the statement has.
    private static readonly object TransactionBlockResponseFormat = new
    {
        type = "object",
        properties = new
        {
            type = new { type = "string", @enum = new[] { "Return", "Purchase" } },
            transactionDate = new { type = "string" },
            referenceNumber = new { type = "string" },
            description = new { type = "string" },
            amount = new { type = "number" }
        },
        required = new[] { "type", "transactionDate", "description", "amount" }
    };

    public AmazonSynchronyBankAnalyzer(IAiPlatform aiPlatform)
    {
        _aiPlatform = aiPlatform;
    }

    public async Task<AnalyzeStatementResponse> AnalyzeStatement(string statementText)
    {
        var response = new AnalyzeStatementResponse
        {
            Status = ResponseStatus.Failure,
            CreditCardStatement = new CreditCardStatementDto()
        };

        try
        {
            using var headerTokenSource = new CancellationTokenSource(_timeout);
            var headerResponse = await _aiPlatform.GenerateAsync(new GenerateRequest
            {
                Model = "qwen3:8b",
                Prompt = $"{AmazonSynchronyBankSystemPrompt.HeaderSystemMessage}\n\n{statementText}",
                Stream = false,
                Format = HeaderResponseFormat,
                Think = false
            }, headerTokenSource.Token);

            var header = JsonSerializer.Deserialize<SynchronyBankHeaderAiResponse>(headerResponse.Response)
                ?? throw new JsonException("AI header response deserialized to null.");

            var transSectionText = ExtractTransactionsSection(statementText);

            // Try the fast, deterministic regex parse first, but only trust it once its
            // "Purchase" total reconciles against the total the statement itself prints in the
            // Account Summary. If the statement's layout ever changes and the regex stops
            // matching correctly, that reconciliation fails and it falls back to the slower but
            // more format-tolerant AI extraction below, instead of silently returning wrong data.
            List<SynchronyBankAiTransaction>? transactions = null;
            if (header.PeriodStartDate is { } periodStart && header.PeriodEndDate is { } periodEnd)
            {
                var regexTransactions = TryParseTransactionsWithRegex(transSectionText, periodStart, periodEnd);
                if (regexTransactions is not null && Reconciles(regexTransactions, header.PurchasesAndOtherDebits))
                {
                    transactions = regexTransactions;
                }
            }

            if (transactions is null)
            {
                var fallbackPeriodStart = header.PeriodStartDate ?? header.PeriodEndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var fallbackPeriodEnd = header.PeriodEndDate ?? fallbackPeriodStart;
                transactions = await ParseTransactionsWithAiAsync(transSectionText, fallbackPeriodStart, fallbackPeriodEnd);
            }

            // A $0.00 line is essentially never a real purchase or return - it's statement
            // boilerplate (an interest/fee summary line, billing-cycle text a lookahead
            // mistook for an item name, etc.) that happened to match the transaction-line
            // shape. Zero doesn't move the reconciliation sum either way, so this slips past
            // that check regardless of which path produced it - filtered here instead. Also
            // requires a reference number, so both conditions must hold - "||" only drops a
            // row when BOTH checks fail at once, which almost nothing does.
            transactions = [.. transactions.Where(transaction => transaction.Amount != 0m)];

            response.CreditCardStatement = MapToStatementDto(header, transactions);
            response.Status = ResponseStatus.Success;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation timed out");
            response.Status = ResponseStatus.Timeout;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return response;
    }

    // The statement's boilerplate before the line items (summary, disclosures, rewards
    // program text, etc.) can easily outweigh the transactions themselves in token count.
    // Falls back to the full text if the marker isn't found, rather than sending nothing.
    private static string ExtractTransactionsSection(string statementText)
    {
        var index = statementText.IndexOf(TransactionSectionMarker, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? statementText[index..] : statementText;
    }

    private static CreditCardStatementDto MapToStatementDto(
        SynchronyBankHeaderAiResponse header,
        List<SynchronyBankAiTransaction> transactions)
    {
        return new CreditCardStatementDto
        {
            IssuerName = "Synchrony Bank",
            LastFourDigits = header.AccountLastFourDigits ?? string.Empty,
            PeriodStartDate = header.PeriodStartDate ?? default,
            PeriodEndDate = header.PeriodEndDate ?? default,
            PaymentDueDate = header.TotalPaymentDue?.DueDate,
            MinimumPaymentDue = header.MinimumPaymentDue?.Amount,
            NewBalance = header.TotalPaymentDue?.Amount ?? 0m,
            Transactions = transactions
                .Select((transaction, index) => new CreditCardTransactionDto
                {
                    TransactionDate = transaction.TransactionDate,
                    RawDescription = transaction.Description,
                    Amount = transaction.Amount,
                    ReferenceNumber = transaction.ReferenceNumber,
                    TransactionType = MapTransactionType(transaction.Type),
                    SourceSequence = index
                })
                .ToList()
        };
    }

    private static TransactionType MapTransactionType(string type) => type switch
    {
        "Purchase" => TransactionType.Purchase,
        "Return" => TransactionType.Refund,
        _ => TransactionType.Unknown
    };

    // Only trusted by the caller once its "Purchase" total reconciles against the statement's
    // own printed total - see the reconciliation check in AnalyzeStatement.
    private static List<SynchronyBankAiTransaction>? TryParseTransactionsWithRegex(
        string transactionSectionText, DateOnly periodStart, DateOnly periodEnd)
    {
        var lines = transactionSectionText.Replace("\r\n", "\n").Split('\n');

        // Parsed per raw occurrence (not deduped up front): this statement's duplicated
        // sections mean the same transaction can appear once cleanly and once mangled (e.g.
        // its real item name buried behind interleaved Account Summary boilerplate). Picking
        // "whichever occurrence comes first" can land on the mangled one; instead, every
        // occurrence gets parsed and the best one per key - the one that actually found a real
        // item name, if any did - wins.
        var byKey = new Dictionary<string, (SynchronyBankAiTransaction Transaction, bool FoundItemName)>();
        var keyOrder = new List<string>();

        foreach (var i in FindTransactionLineIndices(lines))
        {
            var line = lines[i];
            var dateMatch = TransactionLineRegex.Match(line);

            var month = int.Parse(dateMatch.Groups["month"].Value);
            var day = int.Parse(dateMatch.Groups["day"].Value);

            if (month is < 1 or > 12 || day is < 1 or > 31)
            {
                continue;
            }

            var amountMatches = AmountRegex.Matches(line);
            if (amountMatches.Count == 0)
            {
                continue;
            }

            var amountMatch = amountMatches[^1];
            var amountText = amountMatch.Groups["amount"].Value.Replace(",", string.Empty);
            if (!decimal.TryParse(amountText, out var amount))
            {
                continue;
            }

            var isNegative = amountMatch.Groups["sign"].Success;

            var afterDateIndex = GetAfterDateIndex(line, dateMatch);
            var rest = line[afterDateIndex..amountMatch.Index].Trim();

            string? referenceNumber = null;
            var refMatch = ReferenceNumberRegex.Match(rest);
            if (refMatch.Success)
            {
                referenceNumber = refMatch.Groups["ref"].Value;
                rest = rest[refMatch.Length..].Trim();
            }

            var (description, foundItemName) = MerchantBoilerplateRegex.IsMatch(rest)
                ? AppendItemName(lines, i, rest)
                : (rest, true);

            var transaction = new SynchronyBankAiTransaction
            {
                Type = isNegative ? "Return" : "Purchase",
                TransactionDate = InferTransactionDate(month, day, periodStart, periodEnd),
                ReferenceNumber = referenceNumber,
                Description = description,
                Amount = isNegative ? -amount : amount
            };

            var key = referenceNumber ?? line.Trim();
            if (!byKey.TryGetValue(key, out var existing))
            {
                byKey[key] = (transaction, foundItemName);
                keyOrder.Add(key);
            }
            else if (foundItemName && !existing.FoundItemName)
            {
                byKey[key] = (transaction, foundItemName);
            }
        }

        var transactions = keyOrder.Select(key => byKey[key].Transaction).ToList();
        return transactions.Count > 0 ? transactions : null;
    }

    // Looks at the few lines following a transaction row for its item name, skipping blank
    // lines and watermark noise, and stopping at the next transaction row if no item name
    // turns up before it. Every transaction on this statement is with Amazon, so the item name
    // (when found) replaces the boilerplate merchant name/location entirely rather than being
    // appended after it - that boilerplate is only kept as a fallback when no item name exists.
    private static (string Description, bool FoundItemName) AppendItemName(
        string[] lines, int transactionLineIndex, string merchantFallback)
    {
        // Wider than a typical block needs, so a transaction that lands right at a page break
        // still has enough room to skip past the page header/footer boilerplate that sits
        // between it and its real item name there.
        var lookaheadLimit = Math.Min(lines.Length, transactionLineIndex + 8);
        for (var i = transactionLineIndex + 1; i < lookaheadLimit; i++)
        {
            var rawLine = lines[i];
            if (TransactionLineRegex.IsMatch(rawLine))
            {
                break;
            }

            var candidate = rawLine.Trim();
            if (candidate.Length == 0 || NoiseLineRegex.IsMatch(candidate) || IsAccountSummaryBoilerplate(candidate))
            {
                continue;
            }

            return (candidate, true);
        }

        return (merchantFallback, false);
    }

    private static bool IsAccountSummaryBoilerplate(string candidate) =>
        AccountSummaryBoilerplatePrefixes.Any(prefix =>
            candidate.Contains(prefix, StringComparison.OrdinalIgnoreCase));

    // Transaction lines only print "MM/DD", no year. Tries the billing period's start and end
    // years (covers cycles that span a year boundary, e.g. Dec-Jan) and falls back to the
    // period's end year if neither produces a valid calendar date.
    private static DateOnly InferTransactionDate(int month, int day, DateOnly periodStart, DateOnly periodEnd)
    {
        foreach (var year in new[] { periodStart.Year, periodEnd.Year })
        {
            if (DateOnly.TryParseExact($"{year:D4}-{month:D2}-{day:D2}", "yyyy-MM-dd", out var candidate))
            {
                return candidate;
            }
        }

        return new DateOnly(periodEnd.Year, month, day);
    }

    private static bool Reconciles(List<SynchronyBankAiTransaction> transactions, decimal? expectedPurchasesTotal)
    {
        if (expectedPurchasesTotal is not { } expected)
        {
            return false;
        }

        var actual = transactions
            .Where(transaction => transaction.Type == "Purchase")
            .Sum(transaction => transaction.Amount);

        return Math.Abs(actual - expected) <= 0.01m;
    }

    // Splits the transaction section into one raw, unparsed text block per transaction - from
    // one "MM/DD ... $amount" line up to (but not including) the next one - the same shape
    // TransactionLineRegex already finds boundaries with, just without trying to parse the
    // internal structure of each block here. Blocks are sliced against the full (undeduped)
    // boundary list even though only deduped boundaries get emitted, so a duplicated section
    // stays its own skipped region instead of being absorbed into the preceding real block.
    private static List<string> ExtractTransactionBlocks(string transactionSectionText)
    {
        var lines = transactionSectionText.Replace("\r\n", "\n").Split('\n');
        var allBoundaries = FindTransactionLineIndices(lines);
        var keptBoundaries = DeduplicateIndices(lines, allBoundaries);

        var blocks = new List<string>();
        foreach (var start in keptBoundaries)
        {
            var position = allBoundaries.IndexOf(start);
            var end = position + 1 < allBoundaries.Count ? allBoundaries[position + 1] : lines.Length;
            blocks.Add(string.Join('\n', lines[start..end]));
        }

        return blocks;
    }

    // All lines that look like the start of a transaction, in order, including duplicates -
    // see DeduplicateIndices for the actual dedup.
    private static List<int> FindTransactionLineIndices(string[] lines)
    {
        var indices = new List<int>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (TransactionLineRegex.IsMatch(lines[i]))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    // Some statements' PDF text extraction duplicates whole sections verbatim (the same
    // transaction lines appearing twice), which - left alone - both inflates the regex path's
    // reconciliation sum (causing it to wrongly fall back to AI) and produces duplicate
    // transactions either way. Dedupes by reference number when present - it's unique per real
    // transaction, so two lines sharing one are certainly the same transaction, unlike
    // comparing amount/description, which two genuinely distinct transactions could legitimately
    // share. Falls back to the exact trimmed line text for entries with no reference number
    // (payments, fees, interest charges), which don't legitimately repeat word-for-word within
    // one statement.
    private static List<int> DeduplicateIndices(string[] lines, List<int> indices)
    {
        var seenKeys = new HashSet<string>();
        var kept = new List<int>();

        foreach (var index in indices)
        {
            if (seenKeys.Add(BuildDedupKey(lines, index)))
            {
                kept.Add(index);
            }
        }

        return kept;
    }

    private static string BuildDedupKey(string[] lines, int index)
    {
        var line = lines[index];
        var dateMatch = TransactionLineRegex.Match(line);
        var afterDateIndex = GetAfterDateIndex(line, dateMatch);
        var refMatch = ReferenceNumberRegex.Match(line[afterDateIndex..]);
        return refMatch.Success ? refMatch.Groups["ref"].Value : line.Trim();
    }

    // Position of the first non-whitespace character after the "MM/DD" date on a transaction
    // line - i.e. where the reference number/description portion of the line begins.
    private static int GetAfterDateIndex(string line, Match dateMatch)
    {
        var index = dateMatch.Groups["day"].Index + dateMatch.Groups["day"].Length;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }
        return index;
    }

    // The AI fallback: one small, simple call per transaction block instead of one large call
    // parsing the whole list at once. Sequential, not concurrent, since concurrent calls
    // compete for the same local Ollama server/GPU and end up slower overall, not faster.
    private async Task<List<SynchronyBankAiTransaction>> ParseTransactionsWithAiAsync(
        string transactionSectionText, DateOnly periodStart, DateOnly periodEnd)
    {
        var blocks = ExtractTransactionBlocks(transactionSectionText);
        var transactions = new List<SynchronyBankAiTransaction>(blocks.Count);
        var systemMessage = AmazonSynchronyBankSystemPrompt.BuildTransactionBlockSystemMessage(periodStart, periodEnd);

        foreach (var block in blocks)
        {
            using var blockTokenSource = new CancellationTokenSource(_blockTimeout);
            var blockResponse = await _aiPlatform.GenerateAsync(new GenerateRequest
            {
                Model = "qwen3:8b",
                Prompt = $"{systemMessage}\n\n{block}",
                Stream = false,
                Format = TransactionBlockResponseFormat,
                Think = false
            }, blockTokenSource.Token);

            var transaction = JsonSerializer.Deserialize<SynchronyBankAiTransaction>(blockResponse.Response)
                ?? throw new JsonException("AI transaction-block response deserialized to null.");
            transactions.Add(transaction);
            Console.WriteLine($"Parsed transaction: {transaction}");
        }
        return transactions;
    }
}
