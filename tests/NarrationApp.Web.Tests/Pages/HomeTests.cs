using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Pages;

namespace NarrationApp.Web.Tests.Pages;

public sealed class HomeTests : TestContext
{
    [Fact]
    public void Unauthenticated_users_are_redirected_to_login()
    {
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())));
        var navigation = Services.GetRequiredService<NavigationManager>();

        RenderComponent<Home>();

        Assert.EndsWith("/auth/login", navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Authenticated_admin_users_are_redirected_to_admin_dashboard()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@narration.app"),
            new Claim(ClaimTypes.Role, "admin")
        ], "Test");

        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider(new ClaimsPrincipal(identity)));
        var navigation = Services.GetRequiredService<NavigationManager>();

        RenderComponent<Home>();

        Assert.EndsWith("/admin/dashboard", navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state = new(principal);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(_state);
        }
    }
}
