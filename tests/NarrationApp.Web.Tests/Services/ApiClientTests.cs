using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class ApiClientTests
{
    [Fact]
    public async Task GetAsync_logs_user_out_when_protected_endpoint_returns_unauthorized()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "admin@narration.app",
                PreferredLanguage = "vi",
                Role = Shared.Enums.UserRole.Admin,
                Token = "expired-token"
            }
        };
        var authStateProvider = new CustomAuthStateProvider(store);
        var sut = new ApiClient(new HttpClient(new ProtectedUnauthorizedHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store, authStateProvider);

        var exception = await Assert.ThrowsAsync<ApiException>(() => sut.GetAsync<object>("api/admin/stats/overview"));
        var state = await authStateProvider.GetAuthenticationStateAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Null(store.Session);
        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task PutAsync_keeps_session_when_business_unauthorized_error_is_returned()
    {
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "owner@narration.app",
                PreferredLanguage = "vi",
                Role = Shared.Enums.UserRole.PoiOwner,
                Token = "jwt-token"
            }
        };
        var authStateProvider = new CustomAuthStateProvider(store);
        var sut = new ApiClient(new HttpClient(new InvalidCurrentPasswordHandler())
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store, authStateProvider);

        var exception = await Assert.ThrowsAsync<ApiException>(() => sut.PutAsync("api/auth/change-password", new { currentPassword = "wrong" }));
        var state = await authStateProvider.GetAuthenticationStateAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.NotNull(store.Session);
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("owner@narration.app", state.User.FindFirst(ClaimTypes.Name)?.Value);
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

    private sealed class ProtectedUnauthorizedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }

    private sealed class InvalidCurrentPasswordHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = JsonContent.Create(new ApiResponse<object?>
                {
                    Succeeded = false,
                    Message = "Password change failed.",
                    Error = new ErrorResponse
                    {
                        Code = "invalid_current_password",
                        Message = "Current password is invalid."
                    }
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            });
        }
    }
}
