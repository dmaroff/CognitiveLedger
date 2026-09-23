using System.Net.Http.Json;
using System.Text.Json;

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
}
