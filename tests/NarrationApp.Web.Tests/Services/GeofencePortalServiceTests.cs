using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class GeofencePortalServiceTests
{
    [Fact]
    public async Task UpdateAsync_sends_authorized_put_request_and_returns_geofence()
    {
        var handler = new InspectingGeofenceHandler();
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "owner@narration.app",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };

        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, sessionStore);
        var sut = new GeofencePortalService(apiClient);

        var response = await sut.UpdateAsync(7, new UpdateGeofenceRequest
        {
            Name = "Vùng chính",
            RadiusMeters = 35,
            Priority = 9,
            DebounceSeconds = 12,
            CooldownSeconds = 480,
            IsActive = true,
            TriggerAction = "auto_play",
            NearestOnly = true
        });

        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal("api/geofences/7", handler.RequestUri);
        Assert.Contains("Bearer jwt-token", handler.AuthorizationHeader);
        Assert.Contains("\"radiusMeters\":35", handler.SerializedBody);
        Assert.Equal(35, response.RadiusMeters);
        Assert.Equal("Vùng chính", response.Name);
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

    private sealed class InspectingGeofenceHandler : HttpMessageHandler
    {
        public HttpMethod Method { get; private set; } = HttpMethod.Get;

        public string RequestUri { get; private set; } = string.Empty;

        public string AuthorizationHeader { get; private set; } = string.Empty;

        public string SerializedBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty;
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? string.Empty;
            SerializedBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<GeofenceDto>
                {
                    Succeeded = true,
                    Message = "ok",
                    Data = new GeofenceDto
                    {
                        Id = 91,
                        PoiId = 7,
                        Name = "Vùng chính",
                        RadiusMeters = 35,
                        Priority = 9,
                        DebounceSeconds = 12,
                        CooldownSeconds = 480,
                        IsActive = true,
                        TriggerAction = "auto_play",
                        NearestOnly = true
                    }
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
        }
    }
}
