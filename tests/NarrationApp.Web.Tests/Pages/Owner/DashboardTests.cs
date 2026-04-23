using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;
using System.Security.Claims;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class DashboardTests : TestContext
{
    [Fact]
    public void Renders_welcome_banner_with_owner_name_and_key_counts()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Chào Bà Tám Bún Bò", cut.Markup);
            Assert.Contains("4 POI đang quản lý", cut.Markup);
            Assert.Contains("1 chờ duyệt", cut.Markup);
            Assert.Contains("5 thông báo chưa đọc", cut.Markup);
        });
    }

    [Fact]
    public void Renders_spotlight_published_poi_area()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("POI spotlight", cut.Markup);
            Assert.Contains("Các điểm đang xuất bản", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("2 vùng kích hoạt", cut.Markup);
            Assert.Contains("3 ngôn ngữ", cut.Markup);
            Assert.Contains("Kết hợp", cut.Markup);
        });
    }

    [Fact]
    public void Renders_activity_feed()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Dòng hoạt động", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh đang phục vụ khách", cut.Markup);
            Assert.Contains("Ốc đêm đang ở trạng thái nháp", cut.Markup);
            Assert.Contains("12 audio sẵn sàng", cut.Markup);
        });
    }

    [Fact]
    public void Renders_moderation_watch_summary()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Moderation watch", cut.Markup);
            Assert.Contains("2 yêu cầu chờ xử lý", cut.Markup);
            Assert.Contains("1 POI cần phản hồi kiểm duyệt", cut.Markup);
            Assert.Contains("Mở moderation", cut.Markup);
        });
    }

    [Fact]
    public void Does_not_render_old_readiness_board_only_wording()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Bảng sẵn sàng POI", cut.Markup);
            Assert.DoesNotContain("Mức sẵn sàng POI", cut.Markup);
            Assert.DoesNotContain("Kỷ luật phát hành audio", cut.Markup);
            Assert.DoesNotContain("Sẵn sàng audio", cut.Markup);
        });
    }

    private void ConfigureDashboard(
        string ownerName = "Bà Tám Bún Bò",
        OwnerDashboardDto? dashboard = null,
        IReadOnlyList<PoiDto>? pois = null)
    {
        Services.AddSingleton<AuthenticationStateProvider>(
            new TestAuthenticationStateProvider(ownerName));
        Services.AddSingleton<IOwnerPortalService>(
            new TestOwnerPortalService(dashboard ?? BuildDashboard(), pois ?? BuildPois()));
    }

    private static OwnerDashboardDto BuildDashboard() => new()
    {
        TotalPois = 4,
        PublishedPois = 2,
        DraftPois = 1,
        PendingReviewPois = 1,
        TotalAudioAssets = 12,
        PendingModerationRequests = 2,
        UnreadNotifications = 5
    };

    private static IReadOnlyList<PoiDto> BuildPois() =>
    [
        new PoiDto
        {
            Id = 1,
            Name = "Bún mắm Vĩnh Khánh",
            Slug = "bun-mam-vinh-khanh",
            OwnerId = Guid.NewGuid(),
            Priority = 1,
            NarrationMode = NarrationMode.Both,
            Status = PoiStatus.Published,
            Translations =
            [
                new TranslationDto { Id = 1, LanguageCode = "vi", Title = "Bún mắm" },
                new TranslationDto { Id = 2, LanguageCode = "en", Title = "Fermented noodle soup" },
                new TranslationDto { Id = 3, LanguageCode = "ko", Title = "분맘" }
            ],
            Geofences =
            [
                new GeofenceDto { Id = 1, Name = "North gate", RadiusMeters = 18, IsActive = true },
                new GeofenceDto { Id = 2, Name = "South gate", RadiusMeters = 24, IsActive = true }
            ]
        },
        new PoiDto
        {
            Id = 2,
            Name = "Ốc đêm",
            Slug = "oc-dem",
            OwnerId = Guid.NewGuid(),
            Priority = 2,
            NarrationMode = NarrationMode.RecordedOnly,
            Status = PoiStatus.Draft,
            Translations =
            [
                new TranslationDto { Id = 4, LanguageCode = "vi", Title = "Ốc đêm" }
            ],
            Geofences =
            [
                new GeofenceDto { Id = 3, Name = "Main lane", RadiusMeters = 20, IsActive = true }
            ]
        },
        new PoiDto
        {
            Id = 3,
            Name = "Cơm tấm than hồng",
            Slug = "com-tam-than-hong",
            OwnerId = Guid.NewGuid(),
            Priority = 3,
            NarrationMode = NarrationMode.TtsOnly,
            Status = PoiStatus.PendingReview,
            Translations =
            [
                new TranslationDto { Id = 5, LanguageCode = "vi", Title = "Cơm tấm" },
                new TranslationDto { Id = 6, LanguageCode = "en", Title = "Broken rice" }
            ],
            Geofences =
            [
                new GeofenceDto { Id = 4, Name = "Street corner", RadiusMeters = 16, IsActive = true }
            ]
        }
    ];

    private sealed class TestAuthenticationStateProvider(string ownerName) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "owner@narration.app"),
                new Claim(ClaimTypes.Email, "owner@narration.app"),
                new Claim(ClaimTypes.Role, "poi_owner"),
                new Claim("full_name", ownerName)
            ], "Test");

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private sealed class TestOwnerPortalService(
        OwnerDashboardDto dashboard,
        IReadOnlyList<PoiDto> pois) : IOwnerPortalService
    {
        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(dashboard);
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(pois);
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
