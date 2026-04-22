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
        AssertProfile(profile, handler.ExpectedProfile);
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
        AssertProfile(profile, handler.ExpectedUpdatedProfile);
    }

    private static void AssertProfile(OwnerProfileDto actual, OwnerProfileDto expected)
    {
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Phone, actual.Phone);
        Assert.Equal(expected.ManagedArea, actual.ManagedArea);
        Assert.Equal(expected.PreferredLanguage, actual.PreferredLanguage);
        Assert.Equal(expected.CreatedAtUtc, actual.CreatedAtUtc);
        Assert.Equal(expected.LastLoginAtUtc, actual.LastLoginAtUtc);
        Assert.Equal(expected.ActivitySummary.TotalPois, actual.ActivitySummary.TotalPois);
        Assert.Equal(expected.ActivitySummary.PublishedPois, actual.ActivitySummary.PublishedPois);
        Assert.Equal(expected.ActivitySummary.DraftPois, actual.ActivitySummary.DraftPois);
        Assert.Equal(expected.ActivitySummary.PendingReviewPois, actual.ActivitySummary.PendingReviewPois);
        Assert.Equal(expected.ActivitySummary.TotalAudioAssets, actual.ActivitySummary.TotalAudioAssets);
        Assert.Equal(expected.ActivitySummary.TotalAudioPlays, actual.ActivitySummary.TotalAudioPlays);
        Assert.Equal(expected.ActivitySummary.UnreadNotifications, actual.ActivitySummary.UnreadNotifications);
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
        public OwnerProfileDto ExpectedProfile { get; } = new()
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Owner One",
            Email = "owner@narration.app",
            Phone = "+84 90 123 4567",
            ManagedArea = "District 1",
            PreferredLanguage = "vi",
            CreatedAtUtc = new DateTime(2026, 3, 23, 10, 0, 0, DateTimeKind.Utc),
            LastLoginAtUtc = new DateTime(2026, 4, 22, 4, 0, 0, DateTimeKind.Utc),
            ActivitySummary = new OwnerActivitySummaryDto
            {
                TotalPois = 3,
                PublishedPois = 2,
                DraftPois = 1,
                PendingReviewPois = 0,
                TotalAudioAssets = 2,
                TotalAudioPlays = 1,
                UnreadNotifications = 5
            }
        };

        public OwnerProfileDto ExpectedUpdatedProfile { get; } = new()
        {
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Owner Updated",
            Email = "owner-edit@narration.app",
            Phone = "+84 90 333 4444",
            ManagedArea = "New District",
            PreferredLanguage = "en",
            CreatedAtUtc = new DateTime(2026, 3, 23, 10, 0, 0, DateTimeKind.Utc),
            LastLoginAtUtc = new DateTime(2026, 4, 22, 4, 0, 0, DateTimeKind.Utc),
            ActivitySummary = new OwnerActivitySummaryDto
            {
                TotalPois = 6,
                PublishedPois = 4,
                DraftPois = 1,
                PendingReviewPois = 1,
                TotalAudioAssets = 8,
                TotalAudioPlays = 2,
                UnreadNotifications = 2
            }
        };

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

            var profile = request.Method == HttpMethod.Put ? ExpectedUpdatedProfile : ExpectedProfile;

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
