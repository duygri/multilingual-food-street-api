using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class AnalyticsTests : TestContext
{
    [Fact]
    public void Renders_signal_floor_heat_lane_and_audio_telemetry()
    {
        Services.AddSingleton<IAdminPortalService>(new TestAdminPortalService());

        var cut = RenderComponent<Analytics>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Sàn tín hiệu thời gian thực", cut.Markup);
            Assert.Contains("Làn nhiệt POI", cut.Markup);
            Assert.Contains("Tầng tín hiệu", cut.Markup);
            Assert.Contains("42", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("55", cut.Markup);
            Assert.Contains("980", cut.Markup);
            Assert.Contains("3,600", cut.Markup);
            Assert.DoesNotContain("Phân tích vận hành", cut.Markup);
            Assert.DoesNotContain("Dữ liệu gặp", cut.Markup);
            Assert.DoesNotContain("Signal floor", cut.Markup);
            Assert.DoesNotContain("Peak weight", cut.Markup);
        });
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto
            {
                TotalPois = 42,
                PublishedPois = 30,
                TotalTours = 7,
                TotalAudioAssets = 96,
                PendingModerationRequests = 4,
                UnreadNotifications = 8,
                TopPois =
                [
                    new TopPoiDto { PoiId = 1, PoiName = "Bún mắm Vĩnh Khánh", Visits = 980 },
                    new TopPoiDto { PoiId = 2, PoiName = "Ốc đêm", Visits = 640 }
                ]
            });
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminPoiDto>>(Array.Empty<AdminPoiDto>());
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserSummaryDto>>(Array.Empty<UserSummaryDto>());
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(Array.Empty<ModerationRequestDto>());
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HeatmapPointDto>>(
            [
                new HeatmapPointDto { Lat = 10.758, Lng = 106.701, Weight = 55 }
            ]);
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TopPoiDto>>(
            [
                new TopPoiDto { PoiId = 1, PoiName = "Bún mắm Vĩnh Khánh", Visits = 980 },
                new TopPoiDto { PoiId = 2, PoiName = "Ốc đêm", Visits = 640 }
            ]);
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AudioPlayAnalyticsDto
            {
                TotalAudioPlays = 980,
                TotalListenSeconds = 3600
            });
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
