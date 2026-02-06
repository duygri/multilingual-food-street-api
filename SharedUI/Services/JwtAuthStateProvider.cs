using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Custom AuthenticationStateProvider that uses JWT tokens stored in LocalStorage
    /// </summary>
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IAuthService _authService;

        public JwtAuthStateProvider(ILocalStorageService localStorage, IAuthService authService)
        {
            _localStorage = localStorage;
            _authService = authService;
            
            // Subscribe to auth changes
            _authService.OnAuthStateChanged += NotifyAuthStateChanged;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _authService.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }

        public void NotifyAuthStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                
                foreach (var claim in token.Claims)
                {
                    // Fix Role Claim Type Mismatch
                    if (claim.Type == "role" || claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    }
                    else
                    {
                        claims.Add(claim);
                    }
                }
            }
            catch
            {
                // Invalid token
            }
            return claims;
        }
    }
}
