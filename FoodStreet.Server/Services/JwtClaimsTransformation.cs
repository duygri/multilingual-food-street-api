using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FoodStreet.Server.Services
{
    /// <summary>
    /// Đọc JWT trực tiếp từ Authorization header và thêm role claims
    /// vào ClaimsPrincipal. Bypass hoàn toàn .NET claim mapping.
    /// </summary>
    public class JwtClaimsTransformation : IClaimsTransformation
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtClaimsTransformation(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
                return Task.FromResult(principal);

            // Đọc JWT trực tiếp từ header
            var context = _httpContextAccessor.HttpContext;
            var authHeader = context?.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Task.FromResult(principal);

            try
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                // Thêm role claims từ JWT (cả dạng ngắn và dài)
                foreach (var claim in jwt.Claims.Where(c => c.Type == "role"))
                {
                    if (!identity.HasClaim("role", claim.Value))
                        identity.AddClaim(new Claim("role", claim.Value));
                    if (!identity.HasClaim(ClaimTypes.Role, claim.Value))
                        identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                }

                // Thêm sub claim nếu thiếu
                var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub");
                if (subClaim != null)
                {
                    if (!identity.HasClaim("sub", subClaim.Value))
                        identity.AddClaim(new Claim("sub", subClaim.Value));
                    if (!identity.HasClaim(ClaimTypes.NameIdentifier, subClaim.Value))
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                }

                // Thêm name claim nếu thiếu
                var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "name");
                if (nameClaim != null)
                {
                    if (!identity.HasClaim("name", nameClaim.Value))
                        identity.AddClaim(new Claim("name", nameClaim.Value));
                    if (!identity.HasClaim(ClaimTypes.Name, nameClaim.Value))
                        identity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
                }
            }
            catch
            {
                // Token không hợp lệ — bỏ qua
            }

            return Task.FromResult(principal);
        }
    }
}
