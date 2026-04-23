using Bunit;
using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class PoisTests : TestContext
{
    [Fact]
    public void List_page_searches_filters_and_links_to_create_and_detail_routes()
    {
        var ownerService = new TestOwnerPortalService();
        Services.AddSingleton<IOwnerPortalService>(ownerService);

        var cut = RenderComponent<Pois>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Danh sách POI", cut.Markup);
            Assert.Contains("3 POI", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Ốc đêm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Cơm tấm than hồng", cut.Markup);
        });

        Assert.Equal("/owner/pois/new", cut.Find("a[data-action='create-poi']").GetAttribute("href"));
        Assert.Equal("/owner/pois/1", cut.Find("a[data-action='view-poi-1']").GetAttribute("href"));

        cut.Find("input[data-field='poi-search']").Change("oc-dem");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ốc đêm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Cơm tấm than hồng", cut.Markup);
        });

        cut.Find("input[data-field='poi-search']").Change(string.Empty);
        cut.Find("select[data-field='poi-status-filter']").Change(PoiStatus.Published.ToString());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Ốc đêm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Cơm tấm than hồng", cut.Markup);
            Assert.Contains("2 vùng", cut.Markup);
            Assert.Contains("2 ngôn ngữ", cut.Markup);
        });
    }

    private sealed class TestOwnerPortalService : IOwnerPortalService
    {
        private readonly IReadOnlyList<PoiDto> _pois =
        [
            new PoiDto
            {
                Id = 1,
                Name = "Bún mắm Vĩnh Khánh",
                Slug = "bun-mam-vinh-khanh",
                OwnerId = Guid.NewGuid(),
                Lat = 10.758,
                Lng = 106.701,
                Priority = 10,
                CategoryName = "Hải sản",
                NarrationMode = NarrationMode.Both,
                Description = "Bún mắm đậm vị.",
                TtsScript = "Script",
                Status = PoiStatus.Published,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                Translations =
                [
                    new TranslationDto { Id = 1, PoiId = 1, LanguageCode = "vi", Title = "Bún mắm" },
                    new TranslationDto { Id = 2, PoiId = 1, LanguageCode = "en", Title = "Noodle soup" }
                ],
                Geofences =
                [
                    new GeofenceDto { Id = 1, PoiId = 1, Name = "Cổng chính", RadiusMeters = 35, IsActive = true },
                    new GeofenceDto { Id = 2, PoiId = 1, Name = "Ngã ba", RadiusMeters = 25, IsActive = true }
                ]
            },
            new PoiDto
            {
                Id = 2,
                Name = "Ốc đêm Vĩnh Khánh",
                Slug = "oc-dem-vinh-khanh",
                OwnerId = Guid.NewGuid(),
                Lat = 10.759,
                Lng = 106.702,
                Priority = 8,
                CategoryName = "Ăn vặt",
                NarrationMode = NarrationMode.RecordedOnly,
                Description = "Quán ốc đêm.",
                TtsScript = "Script",
                Status = PoiStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                Translations =
                [
                    new TranslationDto { Id = 3, PoiId = 2, LanguageCode = "vi", Title = "Ốc đêm" }
                ],
                Geofences =
                [
                    new GeofenceDto { Id = 3, PoiId = 2, Name = "Mặt tiền", RadiusMeters = 20, IsActive = true }
                ]
            },
            new PoiDto
            {
                Id = 3,
                Name = "Cơm tấm than hồng",
                Slug = "com-tam-than-hong",
                OwnerId = Guid.NewGuid(),
                Lat = 10.760,
                Lng = 106.703,
                Priority = 6,
                CategoryName = "Cơm",
                NarrationMode = NarrationMode.TtsOnly,
                Description = "Cơm tấm.",
                TtsScript = "Script",
                Status = PoiStatus.PendingReview,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        ];

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerShellSummaryDto());
        }

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerDashboardDto());
        }

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pois);
        }

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pois.Single(item => item.Id == poiId));
        }

        public Task<OwnerPoiStatsDto> GetPoiStatsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerPoiStatsDto { PoiId = poiId });
        }

        public Task<PoiDto> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PoiDto> UpdatePoiAsync(int poiId, UpdatePoiRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeletePoiAsync(int poiId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
