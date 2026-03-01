using System.Security.Claims;

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
                && c.Value == role);
        }

        public static bool IsAdminRole(this ClaimsPrincipal user) => user.HasRole("Admin");
        public static bool IsSellerRole(this ClaimsPrincipal user) => user.HasRole("Seller");

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
