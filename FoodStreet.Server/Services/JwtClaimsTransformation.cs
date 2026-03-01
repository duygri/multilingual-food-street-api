using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FoodStreet.Server.Services
{
    /// <summary>
    /// Đảm bảo role claim tồn tại với CẢ HAI dạng ngắn ("role") và dài (ClaimTypes.Role)
    /// để [Authorize(Roles)] hoạt động bất kể handler JWT nào đang dùng.
    /// </summary>
    public class JwtClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
                return Task.FromResult(principal);

            // Tìm tất cả role claims (cả ngắn và dài)
            var roleValues = identity.Claims
                .Where(c => c.Type == "role" 
                    || c.Type == ClaimTypes.Role
                    || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            // Thêm claim dạng ngắn nếu thiếu
            foreach (var role in roleValues)
            {
                if (!identity.HasClaim("role", role))
                    identity.AddClaim(new Claim("role", role));
                if (!identity.HasClaim(ClaimTypes.Role, role))
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            // Đảm bảo "sub" claim cũng có dạng NameIdentifier
            var subClaim = identity.FindFirst("sub") 
                ?? identity.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim != null)
            {
                if (!identity.HasClaim(ClaimTypes.NameIdentifier, subClaim.Value))
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                if (!identity.HasClaim("sub", subClaim.Value))
                    identity.AddClaim(new Claim("sub", subClaim.Value));
            }

            return Task.FromResult(principal);
        }
    }
}
