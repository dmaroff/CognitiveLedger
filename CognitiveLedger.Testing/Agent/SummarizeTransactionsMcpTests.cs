using System.Text.Json;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Testing.Agent;

public class SummarizeTransactionsMcpTests
{
    [TestCase("category")]
    [TestCase("month")]
    [Explicit("Requires Ledger MCP and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task SummarizeTransactions_ForJuly2026_GroupsAndTotalsAllTransactions(
        string groupBy)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri("http://localhost:5185/mcp"),
                Name = "CognitiveLedger integration tests"
            });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: timeout.Token);

        var result = await client.CallToolAsync(
            "summarize_transactions",
            new Dictionary<string, object?>
            {
                ["fromDate"] = "2026-07-01",
                ["toDate"] = "2026-07-31",
                ["groupBy"] = groupBy,
                ["groupLimit"] = 50
            },
            cancellationToken: timeout.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsError, Is.Not.True);
            Assert.That(result.StructuredContent, Is.Not.Null);
        }

        var structuredJson = JsonSerializer.Serialize(result.StructuredContent);
        using var structuredContent = JsonDocument.Parse(structuredJson);
        var root = structuredContent.RootElement;
        var groups = root.GetProperty("groups").EnumerateArray().ToArray();

        var chargeCount = root.GetProperty("chargeCount").GetInt32();
        var creditCount = root.GetProperty("creditCount").GetInt32();
        var totalCharges = root.GetProperty("totalCharges").GetDecimal();
        var totalCredits = root.GetProperty("totalCredits").GetDecimal();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.GetProperty("transactionCount").GetInt32(), Is.EqualTo(22));
            Assert.That(chargeCount + creditCount, Is.EqualTo(22));
            Assert.That(
                root.GetProperty("netSpending").GetDecimal(),
                Is.EqualTo(totalCharges - totalCredits));
            Assert.That(root.GetProperty("groupedBy").GetString(), Is.EqualTo(groupBy));
            Assert.That(groups, Has.Length.EqualTo(root.GetProperty("totalGroups").GetInt32()));
            Assert.That(groups.Sum(group => group.GetProperty("transactionCount").GetInt32()),
                Is.EqualTo(22));
            Assert.That(groups.Sum(group => group.GetProperty("totalCharges").GetDecimal()),
                Is.EqualTo(totalCharges));
            Assert.That(groups.Sum(group => group.GetProperty("totalCredits").GetDecimal()),
                Is.EqualTo(totalCredits));
        }
    }
}