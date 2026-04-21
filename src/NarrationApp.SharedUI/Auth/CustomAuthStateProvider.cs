using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using NarrationApp.Shared.Enums;

namespace NarrationApp.SharedUI.Auth;

public sealed class CustomAuthStateProvider(IAuthSessionStore sessionStore) : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await sessionStore.GetAsync();
        return session is null ? AnonymousState : CreateAuthenticationState(session);
    }

    public async Task MarkUserAsAuthenticatedAsync(AuthSession session)
    {
        await sessionStore.SetAsync(session);
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(session)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await sessionStore.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState));
    }

    private static AuthenticationState CreateAuthenticationState(AuthSession session)
    {
        if (string.IsNullOrWhiteSpace(session.Token))
        {
            return AnonymousState;
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Email),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.Role, ToRoleName(session.Role)),
            new Claim("preferred_language", session.PreferredLanguage)
        ], "jwt");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static string ToRoleName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "admin",
            UserRole.PoiOwner => "poi_owner",
            UserRole.Tourist => "tourist",
            _ => "tourist"
        };
    }
}
