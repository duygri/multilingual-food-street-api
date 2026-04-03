using System.Security.Claims;

namespace FoodStreet.Client.Constants;

public static class UiRoles
{
    public const string Admin = "Admin";
    public const string PoiOwner = "POI Owner";
    public const string LegacyPoiOwner = "Seller";
    public const string Tourist = "Tourist";
    public const string LegacyTourist = "User";

    public const string PoiOwnerCompat = PoiOwner + "," + LegacyPoiOwner;
    public const string AdminOrPoiOwnerCompat = Admin + "," + PoiOwner + "," + LegacyPoiOwner;

    public static bool IsPoiOwner(ClaimsPrincipal? user)
    {
        return user is not null && (user.IsInRole(PoiOwner) || user.IsInRole(LegacyPoiOwner));
    }

    public static bool IsPoiOwner(IEnumerable<string>? roles)
    {
        if (roles is null)
        {
            return false;
        }

        return roles.Contains(PoiOwner, StringComparer.OrdinalIgnoreCase)
            || roles.Contains(LegacyPoiOwner, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsTourist(ClaimsPrincipal? user)
    {
        return user is not null && (user.IsInRole(Tourist) || user.IsInRole(LegacyTourist));
    }

    public static string NormalizeRole(string? role)
    {
        return role switch
        {
            LegacyPoiOwner => PoiOwner,
            LegacyTourist => Tourist,
            _ => role ?? string.Empty
        };
    }
}
