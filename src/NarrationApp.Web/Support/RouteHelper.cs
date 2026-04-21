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
}
