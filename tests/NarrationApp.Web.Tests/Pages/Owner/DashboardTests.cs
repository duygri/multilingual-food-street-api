using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class DashboardTests : TestContext
{
    [Fact]
    public void Renders_owner_readiness_panels_and_poi_board()
    {
        Services.AddSingleton<IOwnerPortalService>(new TestOwnerPortalService());

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bảng sẵn sàng POI", cut.Markup);
            Assert.Contains("Theo dõi kiểm duyệt", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("2 vùng kích hoạt", cut.Markup);
            Assert.Contains("3 ngôn ngữ", cut.Markup);
            Assert.Contains("Đã xuất bản", cut.Markup);
            Assert.Contains("Nháp", cut.Markup);
            Assert.DoesNotContain("Trung tâm vận hành owner", cut.Markup);
            Assert.DoesNotContain("Owner studio", cut.Markup);
            Assert.DoesNotContain("Mở POI studio", cut.Markup);
            Assert.DoesNotContain("Published", cut.Markup);
            Assert.DoesNotContain("Pending review", cut.Markup);
        });
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerDashboardDto
            {
                TotalPois = 4,
                PublishedPois = 2,
                DraftPois = 1,
                PendingReviewPois = 1,
                TotalAudioAssets = 12,
                PendingModerationRequests = 2,
                UnreadNotifications = 5
            });
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiDto>>(
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
                }
            ]);
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
