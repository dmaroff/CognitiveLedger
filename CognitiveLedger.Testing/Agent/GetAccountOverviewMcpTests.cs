using System.Text.Json;
using ModelContextProtocol.Client;

namespace CognitiveLedger.Testing.Agent;

public class GetAccountOverviewMcpTests
{
    [Test]
    [Explicit("Requires Ledger MCP and the development PostgreSQL database.")]
    [Category("Integration")]
    public async Task GetAccountOverview_AsOfSeptember2026_ReturnsLatestImportedStatementPerAccount()
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
            "get_account_overview",
            new Dictionary<string, object?>
            {
                ["asOfDate"] = "2026-09-30"
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
        var accounts = root.GetProperty("accounts").EnumerateArray().ToArray();
        var accountCount = root.GetProperty("accountCount").GetInt32();

        var uniqueAccounts = accounts
            .Select(account =>
                $"{account.GetProperty("issuer").GetString()}|" +
                account.GetProperty("accountName").GetString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var statementBalanceTotal = accounts.Sum(account =>
            account.GetProperty("latestStatementBalance").GetDecimal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accountCount, Is.EqualTo(2));
            Assert.That(accounts, Has.Length.EqualTo(accountCount));
            Assert.That(uniqueAccounts, Is.EqualTo(accountCount));
            Assert.That(
                root.GetProperty("totalLatestStatementBalance").GetDecimal(),
                Is.EqualTo(statementBalanceTotal));
            Assert.That(accounts.All(account =>
                    account.GetProperty("statementPeriodEnd").GetDateTime() <=
                    new DateTime(2026, 9, 30)),
                Is.True);
        }
    }
}
