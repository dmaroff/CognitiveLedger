using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using CognitiveLedger.AI.OpenAI.Request;
using CognitiveLedger.AI.OpenAI.Response;
using CognitiveLedger.Parser.PDF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.AI.OpenAI;

public sealed class OpenAiPdfStatementReader : IOpenAiPdfStatementReader
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiPdfOptions _options;
    private readonly ILogger<OpenAiPdfStatementReader> _logger;

    public OpenAiPdfStatementReader(
        HttpClient httpClient,
        OpenAiPdfOptions options,
        ILogger<OpenAiPdfStatementReader>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<OpenAiPdfStatementReader>.Instance;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("An OpenAI API key is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new ArgumentException("An OpenAI model is required.", nameof(options));
        }
    }

    public async Task<ExtractPdfStatementResponse> ExtractAsync(
        ExtractPdfStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PdfData is null || request.PdfData.Length == 0)
        {
            throw new ArgumentException("PDF data cannot be null or empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SummaryPrompt))
        {
            throw new ArgumentException("Summary prompt cannot be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.TransactionPrompt))
        {
            throw new ArgumentException("Transaction prompt cannot be empty.", nameof(request));
        }

        ArgumentNullException.ThrowIfNull(request.SummarySchema);
        ArgumentNullException.ThrowIfNull(request.TransactionSchema);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting {OperationName} for a PDF containing {PdfByteCount} bytes",
            nameof(ExtractAsync),
            request.PdfData.Length);

        try
        {
            var summary = await ExtractSummaryAsync(
                request.PdfData,
                request.SummaryPrompt,
                request.SummarySchema,
                cancellationToken);
            var pages = PdfPageSplitter.SplitPages(request.PdfData, _logger);
            var transactions = new List<ExtractedPageTransaction>();

            for (var index = 0; index < pages.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageTransactions = await ExtractPageTransactionsAsync(
                    pages[index],
                    index + 1,
                    pages.Count,
                    summary,
                    request.TransactionPrompt,
                    request.TransactionSchema,
                    cancellationToken);

                var retryReason = GetPageRetryReason(
                    pageTransactions,
                    transactions.Count > 0);

                if (retryReason is not null)
                {
                    _logger.LogWarning(
                        "Retrying OpenAI transaction extraction for PDF page {PageNumber} of " +
                        "{PageCount}; reason={RetryReason}",
                        index + 1,
                        pages.Count,
                        retryReason);

                    pageTransactions = await ExtractPageTransactionsAsync(
                        pages[index],
                        index + 1,
                        pages.Count,
                        summary,
                        request.TransactionPrompt +
                        " This is a corrective retry. Carefully inspect every visible ledger " +
                        "row, including continuation rows. Do not copy section totals onto " +
                        "transactions, and verify every decimal amount digit by digit.",
                        request.TransactionSchema,
                        cancellationToken);
                }

                for (var transactionIndex = 0;
                     transactionIndex < pageTransactions.Count;
                     transactionIndex++)
                {
                    var transaction = pageTransactions[transactionIndex];
                    _logger.LogInformation(
                        "OpenAI transaction page={PageNumber} row={RowNumber} date={TransactionDate} " +
                        "amount={Amount:F2} isCredit={IsCredit} category={Category} " +
                        "merchant={Merchant} description={Description}",
                        index + 1,
                        transactionIndex + 1,
                        transaction.Date?.ToString("yyyy-MM-dd") ?? "null",
                        transaction.Amount,
                        transaction.IsCredit,
                        transaction.Category,
                        transaction.Merchant,
                        transaction.Description);
                }

                transactions.AddRange(pageTransactions);
                _logger.LogInformation(
                    "Extracted {TransactionCount} transactions from PDF page {PageNumber} of {PageCount}",
                    pageTransactions.Count,
                    index + 1,
                    pages.Count);
            }

            var response = BuildResponse(summary, transactions);
            ValidateStatement(response.Statement);
            stopwatch.Stop();
            _logger.LogInformation(
                "Completed {OperationName} with {TransactionCount} transactions in {ElapsedMilliseconds} ms",
                nameof(ExtractAsync),
                response.Statement.Transactions.Count,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "Failed {OperationName} after {ElapsedMilliseconds} ms",
                nameof(ExtractAsync),
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<ExtractedStatementSummary> ExtractSummaryAsync(
        byte[] pdfData,
        string summaryPrompt,
        object summarySchema,
        CancellationToken cancellationToken)
    {
        var outputText = await SendPdfRequestAsync(
            pdfData,
            "sanitized-statement.pdf",
            summaryPrompt,
            "credit_card_statement_summary",
            summarySchema,
            cancellationToken);

        try
        {
            var response = JsonSerializer.Deserialize<ExtractStatementSummaryResponse>(
                outputText,
                SerializerOptions);

            return response?.StatementSummary
                   ?? throw new OpenAiPdfStatementException(
                       "OpenAI returned an empty statement summary response.");
        }
        catch (OpenAiPdfStatementException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new OpenAiPdfStatementException(
                "OpenAI returned a statement summary that could not be parsed.",
                exception);
        }
    }

    private async Task<IReadOnlyList<ExtractedPageTransaction>> ExtractPageTransactionsAsync(
        byte[] pagePdfData,
        int pageNumber,
        int pageCount,
        ExtractedStatementSummary summary,
        string transactionPrompt,
        object transactionSchema,
        CancellationToken cancellationToken)
    {
        var prompt =
            $"{transactionPrompt} This is page {pageNumber} of {pageCount}. " +
            $"The statement period is {summary.StatementPeriodStart:yyyy-MM-dd} through " +
            $"{summary.StatementPeriodEnd:yyyy-MM-dd}.";

        var outputText = await SendPdfRequestAsync(
            pagePdfData,
            $"sanitized-statement-page-{pageNumber}.pdf",
            prompt,
            "credit_card_page_transactions",
            transactionSchema,
            cancellationToken);

        try
        {
            var response = JsonSerializer.Deserialize<ExtractPageTransactionsResponse>(
                outputText,
                SerializerOptions);

            return response?.Transactions
                   ?? throw new OpenAiPdfStatementException(
                       $"OpenAI returned an empty response for statement page {pageNumber}.");
        }
        catch (OpenAiPdfStatementException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new OpenAiPdfStatementException(
                $"OpenAI returned transactions for page {pageNumber} that could not be parsed.",
                exception);
        }
    }

    private async Task<string> SendPdfRequestAsync(
        byte[] pdfData,
        string filename,
        string prompt,
        string schemaName,
        object schema,
        CancellationToken cancellationToken)
    {
        var apiRequest = new
        {
            model = _options.Model,
            store = false,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_file",
                            filename,
                            file_data = $"data:application/pdf;base64,{Convert.ToBase64String(pdfData)}"
                        },
                        new
                        {
                            type = "input_text",
                            text = prompt
                        }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = schemaName,
                    strict = true,
                    schema
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        httpRequest.Content = JsonContent.Create(apiRequest, options: SerializerOptions);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting OpenAI request {SchemaName} using model {Model} with {PdfByteCount} PDF bytes",
            schemaName,
            _options.Model,
            pdfData.Length);

        try
        {
            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new OpenAiPdfStatementException(
                    $"OpenAI returned HTTP {(int)httpResponse.StatusCode}: {responseBody}");
            }

            _logger.LogInformation(
                "Completed OpenAI request {SchemaName} with HTTP {StatusCode} in {ElapsedMilliseconds} ms",
                schemaName,
                (int)httpResponse.StatusCode,
                stopwatch.ElapsedMilliseconds);

            try
            {
                return GetOutputText(responseBody);
            }
            catch (JsonException exception)
            {
                throw new OpenAiPdfStatementException(
                    "OpenAI returned a response envelope that could not be parsed.",
                    exception);
            }
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "Failed OpenAI request {SchemaName} after {ElapsedMilliseconds} ms",
                schemaName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private ExtractPdfStatementResponse BuildResponse(
        ExtractedStatementSummary summary,
        IReadOnlyList<ExtractedPageTransaction> transactions)
    {
        var normalizedTransactions = transactions
            .Select((transaction, index) => new ExtractedTransaction
            {
                Id = (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Date = transaction.Date,
                Category = transaction.Category,
                Merchant = transaction.Merchant,
                Description = transaction.Description,
                Amount = Math.Abs(transaction.Amount),
                IsCredit = transaction.IsCredit
            })
            .ToArray();

        var statementSubtotals = normalizedTransactions
            .Where(IsStatementSubtotal)
            .ToArray();

        var zeroAmountArtifacts = normalizedTransactions
            .Where(transaction => transaction.Amount == 0)
            .ToArray();

        if (statementSubtotals.Length > 0)
        {
            _logger.LogWarning(
                "Ignored {SubtotalCount} statement section total(s) returned as transactions",
                statementSubtotals.Length);
        }

        if (zeroAmountArtifacts.Length > 0)
        {
            _logger.LogWarning(
                "Ignored {ArtifactCount} zero-amount statement row(s): {Descriptions}",
                zeroAmountArtifacts.Length,
                string.Join("; ", zeroAmountArtifacts.Select(transaction =>
                    $"{transaction.Category}: {transaction.Description}")));
        }

        var ledgerTransactions = normalizedTransactions
            .Where(transaction =>
                transaction.Amount > 0 &&
                !IsStatementSubtotal(transaction))
            .Select((transaction, index) => new ExtractedTransaction
            {
                Id = (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Date = transaction.Date,
                Category = transaction.Category,
                Merchant = transaction.Merchant,
                Description = transaction.Description,
                Amount = transaction.Amount,
                IsCredit = transaction.IsCredit
            })
            .ToArray();

        var expectedPurchases =
            summary.NewBalance -
            summary.PreviousBalance -
            Math.Abs(summary.Fees) -
            Math.Abs(summary.InterestCharged) +
            Math.Abs(summary.TotalPayments) +
            Math.Abs(summary.TotalOtherCredits);

        var transactionsWithoutDerivedSubtotal = RemoveDerivedPurchaseSubtotal(
            expectedPurchases,
            ledgerTransactions);

        var reconciledTransactions = CollapseSplitPurchaseRow(
            expectedPurchases,
            transactionsWithoutDerivedSubtotal);

        var calculatedTotalPurchases = reconciledTransactions
            .Where(transaction =>
                !transaction.IsCredit &&
                !IsFeeOrInterest(transaction))
            .Sum(transaction => transaction.Amount);

        return new ExtractPdfStatementResponse
        {
            Statement = new ExtractedStatement
            {
                Issuer = summary.Issuer,
                AccountName = summary.AccountName,
                StatementPeriodStart = summary.StatementPeriodStart,
                StatementPeriodEnd = summary.StatementPeriodEnd,
                PreviousBalance = summary.PreviousBalance,
                NewBalance = summary.NewBalance,
                TotalPurchases = calculatedTotalPurchases,
                TotalPayments = Math.Abs(summary.TotalPayments),
                TotalOtherCredits = Math.Abs(summary.TotalOtherCredits),
                Fees = Math.Abs(summary.Fees),
                InterestCharged = Math.Abs(summary.InterestCharged),
                Transactions = reconciledTransactions
            }
        };
    }

    private IReadOnlyList<ExtractedTransaction> CollapseSplitPurchaseRow(
        decimal expectedPurchases,
        IReadOnlyList<ExtractedTransaction> transactions)
    {
        const decimal tolerance = 0.01m;
        var extractedPurchases = transactions
            .Where(transaction => !transaction.IsCredit && !IsFeeOrInterest(transaction))
            .Sum(transaction => transaction.Amount);
        var excess = extractedPurchases - expectedPurchases;

        if (excess <= tolerance)
        {
            return transactions;
        }

        var candidates = transactions
            .Where(transaction =>
                !transaction.IsCredit &&
                !IsFeeOrInterest(transaction) &&
                transaction.Date is not null &&
                Math.Abs(transaction.Amount - excess) <= tolerance)
            .GroupBy(transaction => new
            {
                transaction.Date,
                transaction.Amount,
                Merchant = transaction.Merchant.Trim()
            })
            .Where(group => group.Count() == 2)
            .ToArray();

        if (candidates.Length != 1)
        {
            return transactions;
        }

        var splitRows = candidates[0].ToArray();
        var retained = splitRows[0];
        var removed = splitRows[1];
        var mergedDescription = string.Join(
            " / ",
            splitRows
                .Select(transaction => transaction.Description.Trim())
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        _logger.LogWarning(
            "Collapsed an OpenAI-split purchase row using statement reconciliation; " +
            "date={TransactionDate}, amount={Amount:F2}, merchant={Merchant}, descriptions={Descriptions}",
            retained.Date?.ToString("yyyy-MM-dd"),
            retained.Amount,
            retained.Merchant,
            mergedDescription);

        return transactions
            .Where(transaction => !ReferenceEquals(transaction, removed))
            .Select((transaction, index) => new ExtractedTransaction
            {
                Id = (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Date = transaction.Date,
                Category = transaction.Category,
                Merchant = transaction.Merchant,
                Description = ReferenceEquals(transaction, retained)
                    ? mergedDescription
                    : transaction.Description,
                Amount = transaction.Amount,
                IsCredit = transaction.IsCredit
            })
            .ToArray();
    }

    private IReadOnlyList<ExtractedTransaction> RemoveDerivedPurchaseSubtotal(
        decimal expectedPurchases,
        IReadOnlyList<ExtractedTransaction> transactions)
    {
        const decimal tolerance = 0.01m;
        var debitTransactions = transactions
            .Where(transaction =>
                !transaction.IsCredit &&
                !IsFeeOrInterest(transaction))
            .ToArray();

        if (debitTransactions.Length < 2)
        {
            return transactions;
        }

        var candidates = debitTransactions
            .Where(transaction =>
                transaction.Date is null &&
                Math.Abs(transaction.Amount - expectedPurchases) <= tolerance)
            .ToArray();

        if (candidates.Length != 1)
        {
            return transactions;
        }

        var subtotal = candidates[0];
        _logger.LogWarning(
            "Ignored a derived purchase subtotal returned as a transaction; " +
            "amount={SubtotalAmount:F2}, description={Description}",
            subtotal.Amount,
            subtotal.Description);

        return transactions
            .Where(transaction => !ReferenceEquals(transaction, subtotal))
            .Select((transaction, index) => new ExtractedTransaction
            {
                Id = (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Date = transaction.Date,
                Category = transaction.Category,
                Merchant = transaction.Merchant,
                Description = transaction.Description,
                Amount = transaction.Amount,
                IsCredit = transaction.IsCredit
            })
            .ToArray();
    }

    private static string? GetPageRetryReason(
        IReadOnlyList<ExtractedPageTransaction> pageTransactions,
        bool earlierPageContainedTransactions)
    {
        if (pageTransactions.Count == 0 && earlierPageContainedTransactions)
        {
            return "empty continuation page after transaction rows were already found";
        }

        return null;
    }

    private static bool IsPayment(ExtractedTransaction transaction)
    {
        return IsPayment(transaction.Category, transaction.Merchant, transaction.Description);
    }

    private static bool IsPayment(string category, string merchant, string description)
    {
        var text = string.Join(" ", category, merchant, description);

        return text.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("thank you", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStatementSubtotal(ExtractedTransaction transaction)
    {
        var text = string.Join(
            " ",
            transaction.Category,
            transaction.Merchant,
            transaction.Description);

        return text.Contains("purchases and other debits", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("total purchases", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("total debits", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("fees charged this period", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("interest charged this period", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("statement balance", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFeeOrInterest(ExtractedTransaction transaction)
    {
        var text = string.Join(
            " ",
            transaction.Category,
            transaction.Merchant,
            transaction.Description);

        return Regex.IsMatch(
            text,
            @"\b(?:fees?|interest)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private void ValidateStatement(ExtractedStatement statement)
    {
        const decimal tolerance = 0.01m;

        if (statement.StatementPeriodEnd < statement.StatementPeriodStart)
        {
            throw new OpenAiPdfStatementException(
                "Statement period end precedes its start.");
        }

        if (statement.Transactions.Count == 0)
        {
            throw new OpenAiPdfStatementException(
                "No transactions were extracted from any statement page.");
        }

        if (statement.Transactions.Any(transaction => transaction.Amount <= 0))
        {
            throw new OpenAiPdfStatementException(
                "A transaction amount is not a positive absolute value.");
        }

        if (statement.Transactions.Select(transaction => transaction.Id).Distinct().Count() !=
            statement.Transactions.Count)
        {
            throw new OpenAiPdfStatementException(
                "Duplicate transaction identifiers were generated.");
        }

        var calculatedNewBalance =
            statement.PreviousBalance +
            statement.TotalPurchases +
            statement.Fees +
            statement.InterestCharged -
            statement.TotalPayments -
            statement.TotalOtherCredits;

        if (Math.Abs(calculatedNewBalance - statement.NewBalance) > tolerance)
        {
            throw new OpenAiPdfStatementException(
                $"Statement totals do not reconcile. Previous balance " +
                $"{statement.PreviousBalance:F2} + purchases {statement.TotalPurchases:F2} + " +
                $"fees {statement.Fees:F2} + interest {statement.InterestCharged:F2} - " +
                $"payments {statement.TotalPayments:F2} - other credits " +
                $"{statement.TotalOtherCredits:F2} = " +
                $"{calculatedNewBalance:F2}, but new balance is {statement.NewBalance:F2}.");
        }

        var extractedDebits = statement.Transactions
            .Where(transaction => !transaction.IsCredit)
            .Sum(transaction => transaction.Amount);
        var expectedDebits =
            statement.TotalPurchases + statement.Fees + statement.InterestCharged;

        if (Math.Abs(extractedDebits - expectedDebits) > tolerance)
        {
            throw new OpenAiPdfStatementException(
                $"Extracted debit transactions do not match the statement totals. " +
                $"Expected {expectedDebits:F2}, received {extractedDebits:F2}.");
        }

        var extractedPayments = statement.Transactions
            .Where(transaction => transaction.IsCredit && IsPayment(transaction))
            .Sum(transaction => transaction.Amount);

        if (Math.Abs(extractedPayments - statement.TotalPayments) > tolerance)
        {
            _logger.LogWarning(
                "Extracted payment transactions do not match the authoritative statement " +
                "Payments total; statement={StatementPayments:F2}, transactions={TransactionPayments:F2}",
                statement.TotalPayments,
                extractedPayments);
        }

        var extractedOtherCredits = statement.Transactions
            .Where(transaction => transaction.IsCredit && !IsPayment(transaction))
            .Sum(transaction => transaction.Amount);

        if (Math.Abs(extractedOtherCredits - statement.TotalOtherCredits) > tolerance)
        {
            _logger.LogWarning(
                "Extracted Other Credits transactions do not match the authoritative bold " +
                "statement total; statement={StatementOtherCredits:F2}, transactions={TransactionOtherCredits:F2}",
                statement.TotalOtherCredits,
                extractedOtherCredits);
        }
    }

    private static string GetOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("output", out var output))
        {
            throw new OpenAiPdfStatementException("OpenAI returned no output collection.");
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type) &&
                    type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out var text))
                {
                    return text.GetString()
                           ?? throw new OpenAiPdfStatementException(
                               "OpenAI returned empty output text.");
                }
            }
        }

        throw new OpenAiPdfStatementException("OpenAI returned no statement output text.");
    }
}
