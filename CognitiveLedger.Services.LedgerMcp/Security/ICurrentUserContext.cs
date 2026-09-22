namespace CognitiveLedger.Services.LedgerMcp.Security;

public interface ICurrentUserContext
{
    bool TryGetUserId(out long userId);
}
