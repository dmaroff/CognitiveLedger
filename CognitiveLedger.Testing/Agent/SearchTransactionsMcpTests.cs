using System.Text.Json;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Testing.Agent;

public class SearchTransactionsMcpTests
{
    [Test]
    [Explicit("Requires Ledger MCP and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task SearchTransactions_ForJuly2026_ReturnsTotalMatches()
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
            "search_transactions",
            new Dictionary<string, object?>
            {
                ["fromDate"] = "2026-07-01",
                ["toDate"] = "2026-07-31",
                ["limit"] = 1
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.GetProperty("totalMatches").GetInt32(), Is.EqualTo(22));
            Assert.That(root.GetProperty("transactions").GetArrayLength(), Is.EqualTo(1));
        }
    }
}
