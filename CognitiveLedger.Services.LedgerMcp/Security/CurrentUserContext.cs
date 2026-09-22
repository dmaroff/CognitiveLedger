using System.Globalization;
using System.Security.Claims;
using CognitiveLedger.Data.Models;

namespace CognitiveLedger.Services.LedgerMcp.Security;

public sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment) : ICurrentUserContext
{
    public bool TryGetUserId(out long userId)
    {
        var claimValue = httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (long.TryParse(
                claimValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out userId) &&
            userId > 0)
        {
            return true;
        }
        
        if (environment.IsDevelopment())
        {
            userId = UserCatalog.SystemUserId;
            return true;
        }
        
        userId = 0;
        return false;
    }
}