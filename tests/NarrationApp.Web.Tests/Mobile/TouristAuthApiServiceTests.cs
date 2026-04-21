using System.Net;
using System.Net.Http.Json;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristAuthApiServiceTests
{
    [Fact]
    public async Task LoginAsync_PersistsTouristSession()
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(6);
        var sessionStore = new InMemoryTouristAuthSessionStore();
        var service = CreateService(sessionStore, (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/auth/login-tourist", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Headers.Authorization);

            return Task.FromResult(CreateJsonResponse(new ApiResponse<AuthResponse>
            {
                Succeeded = true,
                Data = new AuthResponse
                {
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "tourist@example.com",
                    PreferredLanguage = "en",
                    Role = UserRole.Tourist,
                    Token = "tourist-token",
                    ExpiresAtUtc = expiresAtUtc
                }
            }));
        });

        var session = await service.LoginAsync(new LoginRequest
        {
            Email = "tourist@example.com",
            Password = "pass123!"
        });

        Assert.Equal("tourist@example.com", session.Email);
        Assert.Equal(UserRole.Tourist, session.Role);
        Assert.Equal("tourist-token", session.Token);

        var persistedSession = await sessionStore.GetAsync();
        Assert.NotNull(persistedSession);
        Assert.Equal("tourist@example.com", persistedSession!.Email);
        Assert.Equal("tourist-token", persistedSession.Token);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_AttachesBearerToken()
    {
        var storedExpiresAtUtc = DateTime.UtcNow.AddHours(3);
        var refreshedExpiresAtUtc = DateTime.UtcNow.AddHours(9);
        var sessionStore = new InMemoryTouristAuthSessionStore(
            new TouristAuthSession(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "tourist@example.com",
                "en",
                UserRole.Tourist,
                "stored-token",
                storedExpiresAtUtc));

        var service = CreateService(sessionStore, (request, cancellationToken) =>
        {
            Assert.Equal("/api/auth/me", request.RequestUri!.AbsolutePath);
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("stored-token", request.Headers.Authorization.Parameter);

            return Task.FromResult(CreateJsonResponse(new ApiResponse<AuthResponse>
            {
                Succeeded = true,
                Data = new AuthResponse
                {
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "tourist@example.com",
                    PreferredLanguage = "en",
                    Role = UserRole.Tourist,
                    Token = "fresh-token",
                    ExpiresAtUtc = refreshedExpiresAtUtc
                }
            }));
        });

        var session = await service.GetCurrentSessionAsync();

        Assert.NotNull(session);
        Assert.Equal("fresh-token", session!.Token);
    }

    private static TouristAuthApiService CreateService(
        ITouristAuthSessionStore sessionStore,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        };

        return new TouristAuthApiService(httpClient, sessionStore);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        };
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }

    private sealed class InMemoryTouristAuthSessionStore(TouristAuthSession? session = null) : ITouristAuthSessionStore
    {
        private TouristAuthSession? _session = session;

        public ValueTask<TouristAuthSession?> GetAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_session);

        public ValueTask SetAsync(TouristAuthSession session, CancellationToken cancellationToken = default)
        {
            _session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            _session = null;
            return ValueTask.CompletedTask;
        }
    }
}
