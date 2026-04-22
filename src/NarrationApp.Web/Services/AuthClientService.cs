using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;

namespace NarrationApp.Web.Services;

public sealed class AuthClientService(ApiClient apiClient, CustomAuthStateProvider authStateProvider)
{
    public async Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<LoginRequest, AuthResponse>("api/auth/login", request, cancellationToken);
        var session = Map(response);
        await authStateProvider.MarkUserAsAuthenticatedAsync(session);
        return session;
    }

    public async Task<AuthSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<RegisterRequest, AuthResponse>("api/auth/register", request, cancellationToken);
        var session = Map(response);
        await authStateProvider.MarkUserAsAuthenticatedAsync(session);
        return session;
    }

    public Task<OwnerRegistrationResponse> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken = default)
    {
        return apiClient.PostAsync<RegisterOwnerRequest, OwnerRegistrationResponse>("api/auth/register-owner", request, cancellationToken);
    }

    public Task LogoutAsync()
    {
        return authStateProvider.MarkUserAsLoggedOutAsync();
    }

    public async Task<AuthSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.GetAsync<AuthResponse>("api/auth/me", cancellationToken);
            var session = Map(response);
            await authStateProvider.MarkUserAsAuthenticatedAsync(session);
            return session;
        }
        catch (ApiException)
        {
            await authStateProvider.MarkUserAsLoggedOutAsync();
            return null;
        }
    }

    public static string GetDefaultRoute(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "/admin/dashboard",
            UserRole.PoiOwner => "/owner/dashboard",
            _ => "/auth/login"
        };
    }

    private static AuthSession Map(AuthResponse response)
    {
        return new AuthSession
        {
            UserId = response.UserId,
            FullName = response.FullName,
            Email = response.Email,
            PreferredLanguage = response.PreferredLanguage,
            Role = response.Role,
            Token = response.Token
        };
    }
}
