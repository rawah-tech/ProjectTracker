using System.Security.Claims;

namespace ProjectTracker.Api.Services;

public static class UserContext
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}
