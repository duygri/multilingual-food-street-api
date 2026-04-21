using System.Security.Claims;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");

        return Guid.Parse(rawUserId);
    }

    public static UserRole GetRequiredUserRole(this ClaimsPrincipal principal)
    {
        var rawRole = principal.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Missing role claim.");

        return rawRole switch
        {
            "admin" => UserRole.Admin,
            "poi_owner" => UserRole.PoiOwner,
            "tourist" => UserRole.Tourist,
            _ => throw new UnauthorizedAccessException($"Unsupported role '{rawRole}'.")
        };
    }
}
