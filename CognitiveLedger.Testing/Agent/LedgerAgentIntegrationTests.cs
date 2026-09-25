using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Testing.Agent;

public class LedgerAgentIntegrationTests
{
    [Test]
    [Explicit("Requires Agents API, Ollama, Ledger MCP, and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task ModelAndAgenticSearch_ForJuly2026_ReturnsTotalMatchesAndRecordsToolExecution()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5043");
        client.Timeout = Timeout.InfiniteTimeSpan;

        using var response = await client.PostAsJsonAsync(
            "/api/agent/messages",
            new
            {
                message =
                    "Use search_transactions to determine how many transactions I had " +
                    "from July 1, 2026 through July 31, 2026. Set limit to 1 and return " +
                    "only the TotalMatches value as an integer."
            },
            timeout.Token);

        var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"Agents API returned {(int)response.StatusCode}: {responseJson}");

        using var responseDocument = JsonDocument.Parse(responseJson);
        var root = responseDocument.RootElement;
        var answer = root.GetProperty("answer").GetString();
        var toolExecutions = root.GetProperty("toolExecutions");

        var searchTransactionsExecution = toolExecutions
            .EnumerateArray()
            .FirstOrDefault(execution =>
                execution.GetProperty("name").GetString() == "search_transactions");

        Assert.That(
            searchTransactionsExecution.ValueKind,
            Is.EqualTo(JsonValueKind.Object),
            "The model did not invoke search_transactions.");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(answer, Does.Contain("22"));
            Assert.That(
                searchTransactionsExecution.GetProperty("succeeded").GetBoolean(),
                Is.True,
                "The search_transactions tool invocation failed.");
        }
    }

    [Test]
    [Explicit("Requires Agents API, Ollama, Ledger MCP, and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task ModelAndAgenticSummary_ForJuly2026_UsesSummarizeTransactionsWithCategoryGrouping()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var expectedSummary = await GetJulySummaryAsync(timeout.Token);

        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5043");
        client.Timeout = Timeout.InfiniteTimeSpan;

        using var response = await client.PostAsJsonAsync(
            "/api/agent/messages",
            new
            {
                message =
                    "How much did I spend from July 1 through July 31, 2026? " +
                    "Break the result down by category."
            },
            timeout.Token);

        var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"Agents API returned {(int)response.StatusCode}: {responseJson}");

        using var responseDocument = JsonDocument.Parse(responseJson);
        var root = responseDocument.RootElement;
        var answer = root.GetProperty("answer").GetString();
        var toolExecutions = root.GetProperty("toolExecutions").EnumerateArray().ToArray();

        var summaryExecution = toolExecutions.FirstOrDefault(execution =>
            execution.GetProperty("name").GetString() == "summarize_transactions");

        Assert.That(
            summaryExecution.ValueKind,
            Is.EqualTo(JsonValueKind.Object),
            "The model did not invoke summarize_transactions.");

        var normalizedAnswer = answer?
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal);
        var expectedNetSpending = expectedSummary.NetSpending.ToString(
            "0.00",
            CultureInfo.InvariantCulture);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                summaryExecution.GetProperty("succeeded").GetBoolean(),
                Is.True,
                "The summarize_transactions tool invocation failed.");
            Assert.That(
                toolExecutions.Any(execution =>
                    execution.GetProperty("name").GetString() == "search_transactions"),
                Is.False,
                "The model should not calculate totals from search_transactions results.");
            Assert.That(normalizedAnswer, Does.Contain(expectedNetSpending));
            Assert.That(answer, Does.Contain(expectedSummary.TopCategory).IgnoreCase);
        }
    }

    [Test]
    [Explicit("Requires Agents API, Ollama, Ledger MCP, and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task ModelAndAgenticAccountOverview_AsOfSeptember2026_UsesLatestImportedStatements()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var expectedOverview = await GetAccountOverviewAsync(timeout.Token);

        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5043");
        client.Timeout = Timeout.InfiniteTimeSpan;

        using var response = await client.PostAsJsonAsync(
            "/api/agent/messages",
            new
            {
                message =
                    "As of September 30, 2026, what balances are shown on my latest imported " +
                    "statement for each account, and what is their total? Include each account " +
                    "name and statement period end date, and make clear these are not live balances."
            },
            timeout.Token);

        var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
        Assert.That(
            response.IsSuccessStatusCode,
            Is.True,
            $"Agents API returned {(int)response.StatusCode}: {responseJson}");

        using var responseDocument = JsonDocument.Parse(responseJson);
        var root = responseDocument.RootElement;
        var answer = root.GetProperty("answer").GetString();
        var toolExecutions = root.GetProperty("toolExecutions").EnumerateArray().ToArray();

        var accountOverviewExecution = toolExecutions.FirstOrDefault(execution =>
            execution.GetProperty("name").GetString() == "get_account_overview");

        Assert.That(
            accountOverviewExecution.ValueKind,
            Is.EqualTo(JsonValueKind.Object),
            "The model did not invoke get_account_overview.");
        Assert.That(answer, Is.Not.Null.And.Not.Empty);

        var normalizedAnswer = answer!
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal);
        var expectedTotal = expectedOverview.TotalLatestStatementBalance.ToString(
            "0.00",
            CultureInfo.InvariantCulture);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                accountOverviewExecution.GetProperty("succeeded").GetBoolean(),
                Is.True,
                "The get_account_overview tool invocation failed.");
            Assert.That(normalizedAnswer, Does.Contain(expectedTotal));
            Assert.That(answer, Does.Contain("statement").IgnoreCase);

            foreach (var account in expectedOverview.Accounts)
            {
                Assert.That(answer, Does.Contain(account.AccountName).IgnoreCase);
                Assert.That(
                    ContainsDate(answer, account.StatementPeriodEnd),
                    Is.True,
                    $"The answer did not include the statement period end date " +
                    $"{account.StatementPeriodEnd:yyyy-MM-dd} for {account.AccountName}.");
            }
        }
    }

    private static async Task<(decimal NetSpending, string TopCategory)> GetJulySummaryAsync(
        CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri("http://localhost:5185/mcp"),
                Name = "CognitiveLedger agent integration tests"
            });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);

        var result = await client.CallToolAsync(
            "summarize_transactions",
            new Dictionary<string, object?>
            {
                ["fromDate"] = "2026-07-01",
                ["toDate"] = "2026-07-31",
                ["groupBy"] = "category"
            },
            cancellationToken: cancellationToken);

        Assert.That(result.IsError, Is.Not.True);
        Assert.That(result.StructuredContent, Is.Not.Null);

        var structuredJson = JsonSerializer.Serialize(result.StructuredContent);
        using var structuredContent = JsonDocument.Parse(structuredJson);
        var root = structuredContent.RootElement;
        var groups = root.GetProperty("groups");

        Assert.That(groups.GetArrayLength(), Is.GreaterThan(0));

        return (
            root.GetProperty("netSpending").GetDecimal(),
            groups[0].GetProperty("key").GetString()!);
    }

    private static async Task<AccountOverviewExpectation> GetAccountOverviewAsync(
        CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri("http://localhost:5185/mcp"),
                Name = "CognitiveLedger agent integration tests"
            });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);

        var result = await client.CallToolAsync(
            "get_account_overview",
            new Dictionary<string, object?>
            {
                ["asOfDate"] = "2026-09-30"
            },
            cancellationToken: cancellationToken);

        Assert.That(result.IsError, Is.Not.True);
        Assert.That(result.StructuredContent, Is.Not.Null);

        var structuredJson = JsonSerializer.Serialize(result.StructuredContent);
        using var structuredContent = JsonDocument.Parse(structuredJson);
        var root = structuredContent.RootElement;
        var accounts = root.GetProperty("accounts")
            .EnumerateArray()
            .Select(account => new AccountStatementExpectation(
                account.GetProperty("accountName").GetString()!,
                DateOnly.Parse(
                    account.GetProperty("statementPeriodEnd").GetString()!,
                    CultureInfo.InvariantCulture)))
            .ToArray();

        Assert.That(accounts, Is.Not.Empty);

        return new AccountOverviewExpectation(
            root.GetProperty("totalLatestStatementBalance").GetDecimal(),
            accounts);
    }

    private static bool ContainsDate(string answer, DateOnly date)
    {
        string[] formats = ["yyyy-MM-dd", "M/d/yyyy", "MMMM d, yyyy", "MMM d, yyyy"];

        return formats.Any(format => answer.Contains(
            date.ToString(format, CultureInfo.GetCultureInfo("en-US")),
            StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AccountOverviewExpectation(
        decimal TotalLatestStatementBalance,
        IReadOnlyList<AccountStatementExpectation> Accounts);

    private sealed record AccountStatementExpectation(
        string AccountName,
        DateOnly StatementPeriodEnd);
}
