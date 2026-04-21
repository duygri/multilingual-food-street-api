using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class DashboardTests : TestContext
{
    [Fact]
    public void Renders_sample_strict_kpis_and_recent_operations_tables()
    {
        Services.AddSingleton<IAdminPortalService>(new TestAdminPortalService());
        Services.AddSingleton<IQrPortalService>(new TestQrPortalService());

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".dashboard-surface"));
            Assert.Contains("Audio Files", cut.Markup);
            Assert.Contains("QR Codes hoạt động", cut.Markup);
            Assert.Contains("Top POI được nghe nhiều nhất", cut.Markup);
            Assert.Contains("Moderation Queue gần đây", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.DoesNotContain("Tuyến ưu tiên của ca trực", cut.Markup);
            Assert.DoesNotContain("Người dùng đang hoạt động", cut.Markup);
        });
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto
            {
                TotalPois = 42,
                PublishedPois = 31,
                PendingModerationRequests = 6,
                TotalTours = 7,
                TotalAudioAssets = 98,
                UnreadNotifications = 11,
                TopPois =
                [
                    new TopPoiDto { PoiId = 1, PoiName = "Bún mắm Vĩnh Khánh", Visits = 980 },
                    new TopPoiDto { PoiId = 2, PoiName = "Ốc đêm", Visits = 640 }
                ]
            });
        }

        public Task<IReadOnlyList<AdminPoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AdminPoiDto>>(Array.Empty<AdminPoiDto>());

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserSummaryDto>>(
            [
                new UserSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Email = "owner@vinhkhanh.vn",
                    RoleName = "poi_owner",
                    PreferredLanguage = "vi",
                    IsActive = true,
                    IsOnline = true,
                    DeviceCount = 3,
                    ActiveDeviceCount = 2,
                    LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-3)
                },
                new UserSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@vinhkhanh.vn",
                    RoleName = "admin",
                    PreferredLanguage = "en",
                    IsActive = true,
                    IsOnline = false,
                    DeviceCount = 2,
                    ActiveDeviceCount = 0,
                    LastSeenAtUtc = DateTime.UtcNow.AddHours(-2)
                }
            ]);
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingModerationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ModerationRequestDto>>(
            [
                new ModerationRequestDto
                {
                    Id = 18,
                    EntityType = "poi",
                    EntityId = "4",
                    RequestedBy = Guid.NewGuid(),
                    Status = ModerationStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
                }
            ]);
        }

        public Task<ModerationRequestDto> ApproveModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ModerationRequestDto> RejectModerationAsync(int requestId, ReviewModerationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HeatmapPointDto>>(Array.Empty<HeatmapPointDto>());

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TopPoiDto>>(Array.Empty<TopPoiDto>());

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AudioPlayAnalyticsDto
            {
                TotalAudioPlays = 1247,
                TotalListenSeconds = 87420
            });

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TestQrPortalService : IQrPortalService
    {
        public Task<IReadOnlyList<QrCodeDto>> GetAsync(string? targetType = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<QrCodeDto>>(
            [
                new QrCodeDto { Id = 1, Code = "QR-001", TargetType = "poi", TargetId = 1, LocationHint = "Khánh Hội" },
                new QrCodeDto { Id = 2, Code = "QR-002", TargetType = "tour", TargetId = 7, LocationHint = "Xóm Chiếu" }
            ]);
        }

        public Task<QrCodeDto> CreateAsync(CreateQrRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int qrId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
