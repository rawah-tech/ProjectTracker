using System.Security.Claims;

namespace ProjectTracker.Api.Services;

public static class UserContext
{
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out userId);
    }
}
