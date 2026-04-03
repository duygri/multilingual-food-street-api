using System.Security.Claims;
using FoodStreet.Server.Constants;

namespace FoodStreet.Server.Extensions
{
    /// <summary>
    /// Extension methods để kiểm tra role từ JWT claims
    /// Hỗ trợ cả tên claim ngắn ("role") và dài (ClaimTypes.Role)
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            return user.Claims.Any(c =>
                (c.Type == "role" 
                 || c.Type == ClaimTypes.Role 
                 || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                && string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsAdminRole(this ClaimsPrincipal user) => user.HasRole(AppRoles.Admin);
        public static bool IsPoiOwnerRole(this ClaimsPrincipal user) => user.HasRole(AppRoles.PoiOwner);
        // Legacy helper retained because the persisted POI Owner role is still stored as "Seller".
        public static bool IsSellerRole(this ClaimsPrincipal user) => user.IsPoiOwnerRole();
        public static bool IsTouristRole(this ClaimsPrincipal user) => user.HasRole(AppRoles.Tourist);

        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user.Claims.FirstOrDefault(c =>
                c.Type == "sub" 
                || c.Type == ClaimTypes.NameIdentifier
                || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            )?.Value;
        }
    }
}
