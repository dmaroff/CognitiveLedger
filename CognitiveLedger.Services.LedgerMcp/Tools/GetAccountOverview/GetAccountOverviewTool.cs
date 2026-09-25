using System.ComponentModel;
using CognitiveLedger.Services.LedgerMcp.Security;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;

[McpServerToolType]
public static class GetAccountOverviewTool
{
    [McpServerTool(
        Name = "get_account_overview",
        Title = "Get account overview",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description(
        "Return the latest imported statement snapshot for each of the current user's accounts. " +
        "Balances are historical statement balances and may not reflect activity after each statement period ended.")]
    public static Task<GetAccountOverviewResult> GetAsync(
        ICurrentUserContext currentUser,
        IAccountOverviewQuery accountOverviewQuery,
        [Description("Statement issuer or partial issuer name, such as Synchrony.")]
        string? issuer = null,
        [Description("Account name or partial account name, such as Amazon or Lowe's.")]
        string? accountName = null,
        [Description(
            "Return the latest imported statement ending on or before this date. " +
            "Omit to use the latest imported statement available for each account.")]
        DateOnly? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.TryGetUserId(out var userId))
        {
            throw new McpException("An authenticated user is required to retrieve an account overview.");
        }

        return accountOverviewQuery.GetAsync(
            new GetAccountOverviewRequest
            {
                UserId = checked((int)userId),
                Arguments = new GetAccountOverviewArguments
                {
                    Issuer = issuer,
                    AccountName = accountName,
                    AsOfDate = asOfDate
                }
            },
            cancellationToken);
    }
}
