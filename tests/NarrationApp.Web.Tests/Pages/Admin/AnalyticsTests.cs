using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Web.Configuration;
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
    public void Analytics_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "Analytics.razor");
        var expectedPartials = new[]
        {
            ("Analytics.razor.cs", "OnInitializedAsync"),
            ("Analytics.Map.razor.cs", "OnAfterRenderAsync"),
            ("Analytics.Filters.razor.cs", "ChangeHeatmapTimeRangeAsync"),
            ("Analytics.Presentation.razor.cs", "FormatDurationShort")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class Analytics", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Analytics.razor.cs")).Length <= 110);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Analytics.Map.razor.cs")).Length <= 100);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Analytics.Filters.razor.cs")).Length <= 150);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "Analytics.Presentation.razor.cs")).Length <= 120);
    }

    [Fact]
    public void Renders_heatmap_flow_and_listening_rankings()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new AnalyticsMapOptions
        {
            AccessToken = "pk.test-token",
            StyleUrl = "mapbox://styles/mapbox/dark-v11"
        });
        var portalService = new TestAdminPortalService();
        Services.AddSingleton<IAdminPortalService>(portalService);

        var cut = RenderComponent<Analytics>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Heatmap vị trí người dùng", cut.Markup);
            Assert.Contains("Tuyến di chuyển ẩn danh", cut.Markup);
            Assert.Contains("Top địa điểm được nghe nhiều nhất", cut.Markup);
            Assert.Contains("Thời gian trung bình nghe 1 POI", cut.Markup);
            Assert.Contains("1,247", cut.Markup);
            Assert.Contains("982", cut.Markup);
            Assert.Contains("365", cut.Markup);
            Assert.Contains("2:34", cut.Markup);
            Assert.Contains("42", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("55", cut.Markup);
            Assert.Contains("3 lượt chuyển", cut.Markup);
            Assert.Contains("154 giây", cut.Markup);
            Assert.Contains("24h", cut.Markup);
            Assert.Contains("7 ngày", cut.Markup);
            Assert.Contains("30 ngày", cut.Markup);
            Assert.Contains("Tất cả", cut.Markup);
            Assert.Contains("Decay", cut.Markup);
            Assert.Contains("Geofence", cut.Markup);
            Assert.Contains("QR", cut.Markup);
            Assert.Contains("Audio", cut.Markup);
            Assert.Contains("Min 2", cut.Markup);
            Assert.Contains("Min 3", cut.Markup);
            Assert.Contains("Min 5", cut.Markup);
            Assert.DoesNotContain("Phân tích vận hành", cut.Markup);
            Assert.DoesNotContain("Signal floor", cut.Markup);
            Assert.DoesNotContain("Peak weight", cut.Markup);

            Assert.NotNull(portalService.LastHeatmapQuery);
            Assert.Equal(HeatmapTimeRange.Last7Days, portalService.LastHeatmapQuery!.TimeRange);
            Assert.Null(portalService.LastHeatmapQuery.EventTypeFilter);
            Assert.True(portalService.LastHeatmapQuery.UseTimeDecay);
            Assert.Equal(50d, portalService.LastHeatmapQuery.GridSizeMeters);
            Assert.Equal(50d, portalService.LastHeatmapQuery.MaxWeight);
            Assert.True(portalService.LastHeatmapQuery.ApplyGaussianSmoothing);
            Assert.False(portalService.UsedLegacyHeatmapEndpoint);

            Assert.NotNull(portalService.LastMovementFlowQuery);
            Assert.Equal(HeatmapTimeRange.Last7Days, portalService.LastMovementFlowQuery!.TimeRange);
            Assert.Null(portalService.LastMovementFlowQuery.EventTypeFilter);
            Assert.Equal(3, portalService.LastMovementFlowQuery.MinimumUniqueSessions);
            Assert.False(portalService.UsedLegacyMovementFlowEndpoint);
        });

        cut.Find("[data-heatmap-event='QrScan']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(portalService.LastHeatmapQuery);
            Assert.Equal(EventType.QrScan, portalService.LastHeatmapQuery!.EventTypeFilter);
        });

        cut.Find("[data-heatmap-range='Last24Hours']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(portalService.LastHeatmapQuery);
            Assert.Equal(HeatmapTimeRange.Last24Hours, portalService.LastHeatmapQuery!.TimeRange);
            Assert.Equal(EventType.QrScan, portalService.LastHeatmapQuery.EventTypeFilter);
        });

        cut.Find("[data-flow-event='AudioPlay']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(portalService.LastMovementFlowQuery);
            Assert.Equal(EventType.AudioPlay, portalService.LastMovementFlowQuery!.EventTypeFilter);
        });

        cut.Find("[data-flow-min-sessions='5']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(portalService.LastMovementFlowQuery);
            Assert.Equal(5, portalService.LastMovementFlowQuery!.MinimumUniqueSessions);
            Assert.Equal(EventType.AudioPlay, portalService.LastMovementFlowQuery.EventTypeFilter);
        });
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        public HeatmapQueryDto? LastHeatmapQuery { get; private set; }

        public MovementFlowQueryDto? LastMovementFlowQuery { get; private set; }

        public bool UsedLegacyHeatmapEndpoint { get; private set; }

        public bool UsedLegacyMovementFlowEndpoint { get; private set; }

        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto
            {
                TotalPois = 42,
                PublishedPois = 30,
                TotalTours = 7,
                TotalAudioAssets = 96,
                PendingModerationRequests = 4,
                UnreadNotifications = 8
            });
        }

        public Task<AnalyticsSnapshotDto> GetAnalyticsSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AnalyticsSnapshotDto
            {
                GeofenceTriggers = 1247,
                AudioPlays = 982,
                QrScans = 365,
                AverageListenDurationSeconds = 154
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

        public Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetVisitorDevicesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VisitorDeviceSummaryDto>>(Array.Empty<VisitorDeviceSummaryDto>());
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
            UsedLegacyHeatmapEndpoint = true;
            return Task.FromResult<IReadOnlyList<HeatmapPointDto>>(
            [
                new HeatmapPointDto { Lat = 10.758, Lng = 106.701, Weight = 55 }
            ]);
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(HeatmapQueryDto query, CancellationToken cancellationToken = default)
        {
            LastHeatmapQuery = query;
            return Task.FromResult<IReadOnlyList<HeatmapPointDto>>(
            [
                new HeatmapPointDto { Lat = 10.758, Lng = 106.701, Weight = 55 }
            ]);
        }

        public Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(CancellationToken cancellationToken = default)
        {
            UsedLegacyMovementFlowEndpoint = true;
            return BuildMovementFlowResponse();
        }

        public Task<IReadOnlyList<MovementFlowDto>> GetMovementFlowsAsync(MovementFlowQueryDto query, CancellationToken cancellationToken = default)
        {
            LastMovementFlowQuery = query;
            return BuildMovementFlowResponse();
        }

        private static Task<IReadOnlyList<MovementFlowDto>> BuildMovementFlowResponse()
        {
            return Task.FromResult<IReadOnlyList<MovementFlowDto>>(
            [
                new MovementFlowDto
                {
                    FromPoiId = 1,
                    FromPoiName = "Bún mắm Vĩnh Khánh",
                    FromLat = 10.758,
                    FromLng = 106.701,
                    ToPoiId = 2,
                    ToPoiName = "Ốc đêm",
                    ToLat = 10.759,
                    ToLng = 106.702,
                    Weight = 3,
                    UniqueSessions = 3
                }
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

        public Task<IReadOnlyList<PoiAverageListenDto>> GetAverageListenByPoiAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PoiAverageListenDto>>(
            [
                new PoiAverageListenDto
                {
                    PoiId = 1,
                    PoiName = "Bún mắm Vĩnh Khánh",
                    AverageListenDurationSeconds = 154,
                    AudioPlayCount = 982
                },
                new PoiAverageListenDto
                {
                    PoiId = 2,
                    PoiName = "Ốc đêm",
                    AverageListenDurationSeconds = 126,
                    AudioPlayCount = 640
                }
            ]);
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
