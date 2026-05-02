using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class DashboardTests : TestContext
{
    [Fact]
    public void Dashboard_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "Dashboard.razor");
        var expectedPartials = new[]
        {
            ("Dashboard.razor.cs", "OnInitializedAsync"),
            ("Dashboard.Presentation.razor.cs", "GetLanguageHint")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class Dashboard", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Dashboard.razor.cs")).Length <= 70);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Dashboard.Presentation.razor.cs")).Length <= 110);
    }

    [Fact]
    public void Renders_sample_strict_kpis_and_recent_operations_tables()
    {
        Services.AddSingleton<IAdminPortalService>(new TestAdminPortalService());
        Services.AddSingleton<IQrPortalService>(new TestQrPortalService());
        Services.AddSingleton<ILanguagePortalService>(new TestLanguagePortalService());

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".dashboard-surface"));
            Assert.Contains("Audio Files", cut.Markup);
            Assert.Contains("QR Codes hoạt động", cut.Markup);
            Assert.Contains("Thiết bị visitor", cut.Markup);
            Assert.Contains("1 đang online", cut.Markup);
            Assert.Contains("Top POI được nghe nhiều nhất", cut.Markup);
            Assert.Contains("Moderation Queue gần đây", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("vi, en, ja", cut.Markup);
            Assert.Contains("77", cut.Markup);
            Assert.DoesNotContain("TB thời gian", cut.Markup);
            Assert.DoesNotContain("Trend", cut.Markup);
            Assert.DoesNotContain("Q5 · Tín ngưỡng", cut.Markup);
            Assert.DoesNotContain("Tuyến ưu tiên của ca trực", cut.Markup);
            Assert.DoesNotContain("Người dùng đang hoạt động", cut.Markup);
            Assert.DoesNotContain("Người dùng", cut.Markup);
            Assert.DoesNotContain("4321", cut.Markup);
            Assert.DoesNotContain("Từ audio plays gần nhất", cut.Markup);
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

        public Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetVisitorDevicesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VisitorDeviceSummaryDto>>(
            [
                new VisitorDeviceSummaryDto
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "Pixel 7",
                    DeviceId = "pixel7-guest-001",
                    PreferredLanguage = "vi-VN",
                    RoleName = "guest",
                    IsOnline = true,
                    LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-2)
                },
                new VisitorDeviceSummaryDto
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "iPhone visitor",
                    DeviceId = "iphone-guest-002",
                    PreferredLanguage = "en-US",
                    RoleName = "guest",
                    IsOnline = false,
                    LastSeenAtUtc = DateTime.UtcNow.AddHours(-1)
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

        public Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsSnapshotDto
            {
                GeofenceTriggers = 144,
                CurrentMonthGeofenceTriggers = 77,
                AudioPlays = 4321,
                QrScans = 365,
                AverageListenDurationSeconds = 154d
            });

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
                TotalAudioPlays = 4321,
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

    private sealed class TestLanguagePortalService : ILanguagePortalService
    {
        public Task<IReadOnlyList<ManagedLanguageDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ManagedLanguageDto>>(
            [
                new ManagedLanguageDto { Code = "vi", DisplayName = "Vietnamese", NativeName = "Tiếng Việt", FlagCode = "VN", IsActive = true },
                new ManagedLanguageDto { Code = "en", DisplayName = "English", NativeName = "English", FlagCode = "GB", IsActive = true },
                new ManagedLanguageDto { Code = "ja", DisplayName = "Japanese", NativeName = "日本語", FlagCode = "JP", IsActive = true }
            ]);
        }

        public Task<ManagedLanguageDto> CreateAsync(CreateManagedLanguageRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string code, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
