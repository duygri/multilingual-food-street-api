using System.Security.Claims;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Support;

public static class RouteHelper
{
    public static string GetDefaultRoute(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "/admin/dashboard",
            UserRole.PoiOwner => "/owner/dashboard",
            _ => "/auth/login"
        };
    }

    public static string GetDefaultRoute(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return role switch
        {
            "admin" => "/admin/dashboard",
            "poi_owner" => "/owner/dashboard",
            _ => "/auth/login"
        };
    }

    public static string GetRoleLabel(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return role switch
        {
            "admin" => "Admin",
            "poi_owner" => "Chủ POI",
            "tourist" => "Khách du lịch (mobile)",
            _ => "Khách"
        };
    }

    public static bool IsOwner(ClaimsPrincipal user)
    {
        return string.Equals(user.FindFirst(ClaimTypes.Role)?.Value, "poi_owner", StringComparison.Ordinal);
    }

    public static string GetDisplayName(ClaimsPrincipal user)
    {
        var fullName = user.FindFirst("full_name")?.Value;
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name ?? "guest@narration.app";
    }
}
