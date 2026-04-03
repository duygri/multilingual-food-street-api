namespace FoodStreet.Server.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    // Persisted legacy value kept for backward compatibility with existing identity/user data.
    public const string PoiOwner = "Seller";
    public const string Tourist = "User";
    public const string AdminOrPoiOwner = Admin + "," + PoiOwner;

    private static readonly string[] AdminAliases = [Admin];
    private static readonly string[] PoiOwnerAliases = [PoiOwner, "POI Owner", "POIOwner", "PoiOwner", "poi_owner"];
    private static readonly string[] TouristAliases = [Tourist, "Tourist", "tourist"];

    public static string NormalizeForPersistence(string? role)
    {
        if (Matches(role, AdminAliases))
        {
            return Admin;
        }

        if (Matches(role, PoiOwnerAliases))
        {
            return PoiOwner;
        }

        return Tourist;
    }

    public static string ToDisplayName(string? persistedRole)
    {
        if (string.Equals(persistedRole, Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        if (string.Equals(persistedRole, PoiOwner, StringComparison.OrdinalIgnoreCase))
        {
            return "POI Owner";
        }

        if (string.Equals(persistedRole, Tourist, StringComparison.OrdinalIgnoreCase))
        {
            return "Tourist";
        }

        return persistedRole ?? string.Empty;
    }

    private static bool Matches(string? role, IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return candidates.Any(candidate => string.Equals(candidate, role, StringComparison.OrdinalIgnoreCase));
    }
}
