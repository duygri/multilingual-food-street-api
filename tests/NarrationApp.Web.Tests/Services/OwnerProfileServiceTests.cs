using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class OwnerProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_loads_owner_profile_data_from_the_api()
    {
        var handler = new InspectingOwnerProfileHandler();
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "owner@narration.app",
                FullName = "Owner One",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };
        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);
        var sut = new OwnerProfileService(apiClient);

        var profile = await sut.GetProfileAsync();

        Assert.Equal(HttpMethod.Get, handler.RequestMethod);
        Assert.Equal("/api/owner/profile", handler.RequestPath);
        Assert.Contains("Bearer jwt-token", handler.AuthorizationHeader);
        Assert.Equal("Owner One", profile.FullName);
        Assert.Equal("District 1", profile.ManagedArea);
        Assert.Equal(3, profile.ActivitySummary.TotalPois);
        Assert.Equal(2, profile.ActivitySummary.TotalAudioAssets);
        Assert.Equal(5, profile.ActivitySummary.UnreadNotifications);
    }

    [Fact]
    public async Task UpdateProfileAsync_sends_owner_profile_payload_and_returns_updated_data()
    {
        var handler = new InspectingOwnerProfileHandler();
        var store = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "owner-edit@narration.app",
                FullName = "Owner Two",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };
        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);
        var sut = new OwnerProfileService(apiClient);

        var profile = await sut.UpdateProfileAsync(new UpdateOwnerProfileRequest
        {
            FullName = "Owner Updated",
            Phone = "+84 90 333 4444",
            ManagedArea = "New District",
            PreferredLanguage = "en"
        });

        Assert.Equal(HttpMethod.Put, handler.RequestMethod);
        Assert.Equal("/api/owner/profile", handler.RequestPath);
        Assert.Contains("Bearer jwt-token", handler.AuthorizationHeader);
        using var payload = JsonDocument.Parse(handler.SerializedBody);
        Assert.Equal("Owner Updated", payload.RootElement.GetProperty("fullName").GetString());
        Assert.Equal("+84 90 333 4444", payload.RootElement.GetProperty("phone").GetString());
        Assert.Equal("New District", payload.RootElement.GetProperty("managedArea").GetString());
        Assert.Equal("en", payload.RootElement.GetProperty("preferredLanguage").GetString());
        Assert.Equal("Owner Updated", profile.FullName);
        Assert.Equal("New District", profile.ManagedArea);
        Assert.Equal(6, profile.ActivitySummary.TotalPois);
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

    private sealed class InspectingOwnerProfileHandler : HttpMessageHandler
    {
        public HttpMethod RequestMethod { get; private set; } = HttpMethod.Get;

        public string RequestPath { get; private set; } = string.Empty;

        public string AuthorizationHeader { get; private set; } = string.Empty;

        public string SerializedBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? string.Empty;
            SerializedBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            var profile = new OwnerProfileDto
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = request.Method == HttpMethod.Put ? "Owner Updated" : "Owner One",
                Email = request.Method == HttpMethod.Put ? "owner-edit@narration.app" : "owner@narration.app",
                Phone = request.Method == HttpMethod.Put ? "+84 90 333 4444" : "+84 90 123 4567",
                ManagedArea = request.Method == HttpMethod.Put ? "New District" : "District 1",
                PreferredLanguage = request.Method == HttpMethod.Put ? "en" : "vi",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                LastLoginAtUtc = DateTime.UtcNow.AddHours(-4),
                ActivitySummary = new OwnerActivitySummaryDto
                {
                    TotalPois = request.Method == HttpMethod.Put ? 6 : 3,
                    PublishedPois = request.Method == HttpMethod.Put ? 4 : 2,
                    DraftPois = request.Method == HttpMethod.Put ? 1 : 1,
                    PendingReviewPois = request.Method == HttpMethod.Put ? 1 : 0,
                    TotalAudioAssets = request.Method == HttpMethod.Put ? 8 : 2,
                    TotalVisits = request.Method == HttpMethod.Put ? 25 : 10,
                    UnreadNotifications = request.Method == HttpMethod.Put ? 2 : 5
                }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<OwnerProfileDto>
                {
                    Succeeded = true,
                    Message = "ok",
                    Data = profile
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
        }
    }
}
