using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;

namespace NarrationApp.Web.Tests.Auth;

public sealed class CustomAuthStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_returns_authenticated_principal_when_session_exists()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                FullName = "System Admin",
                Email = "admin@narration.app",
                Role = UserRole.Admin,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };
        var sut = new CustomAuthStateProvider(store);

        var state = await sut.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("admin@narration.app", state.User.Identity?.Name);
        Assert.Equal("System Admin", state.User.FindFirst("full_name")?.Value);
        Assert.Equal("admin", state.User.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task MarkUserAsLoggedOutAsync_returns_anonymous_principal()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                FullName = "Demo Owner",
                Email = "owner@narration.app",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };
        var sut = new CustomAuthStateProvider(store);

        await sut.MarkUserAsLoggedOutAsync();
        var state = await sut.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Empty(state.User.Claims);
    }

    private sealed class TestAuthSessionStore : IAuthSessionStore
    {
        public AuthSession? Session { get; set; }

        public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Session);
        }

        public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            Session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            Session = null;
            return ValueTask.CompletedTask;
        }
    }
}
