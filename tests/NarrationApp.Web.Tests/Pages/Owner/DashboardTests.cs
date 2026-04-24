using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Web.Pages.Owner;
using NarrationApp.Web.Services;
using System.Security.Claims;

namespace NarrationApp.Web.Tests.Pages.Owner;

public sealed class DashboardTests : TestContext
{
    [Fact]
    public void Dashboard_renders_workspace_stat_cards_published_table_and_recent_activity_panel()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tổng POI", cut.Markup);
            Assert.Contains("Đang xuất bản", cut.Markup);
            Assert.Contains("Chờ duyệt", cut.Markup);
            Assert.Contains("Audio sẵn sàng", cut.Markup);
            Assert.Contains("POI đang xuất bản", cut.Markup);
            Assert.Contains("Bún mắm Vĩnh Khánh", cut.Markup);
            Assert.Contains("Lượt nghe", cut.Markup);
            Assert.Contains("Hoạt động gần đây", cut.Markup);
            Assert.Contains("Admin yêu cầu bổ sung nội dung nguồn", cut.Markup);
        });
    }

    [Fact]
    public void Dashboard_renders_published_workspace_table_headers()
    {
        ConfigureDashboard();

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            var headers = cut.FindAll("th").Select(header => header.TextContent.Trim()).ToArray();

            Assert.Contains("POI", headers);
            Assert.Contains("DANH MỤC", headers);
            Assert.Contains("LƯỢT NGHE", headers);
            Assert.Contains("XU HƯỚNG 7 NGÀY", headers);
            Assert.Contains("VỊ TRÍ", headers);
        });
    }

    [Fact]
    public void Dashboard_renders_empty_recent_activity_panel_when_workspace_has_no_activities()
    {
        ConfigureDashboard(
            workspace: new OwnerDashboardWorkspaceDto
            {
                Summary = new OwnerWorkspaceSummaryDto
                {
                    TotalPois = 1,
                    PublishedPois = 1,
                    PendingReviewPois = 0,
                    ReadyAudioAssets = 0
                },
                PublishedRows =
                [
                    new OwnerDashboardPublishedRowDto
                    {
                        PoiId = 1,
                        PoiName = "Bún mắm Vĩnh Khánh",
                        CategoryName = "Bún",
                        ListenCount = 42,
                        Trend = [1, 2, 3, 4, 5, 6, 7],
                        LocationHint = "Vĩnh Khánh"
                    }
                ],
                RecentActivities = []
            });

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Hoạt động gần đây", cut.Markup);
            Assert.Contains("Chưa có hoạt động nào gần đây.", cut.Markup);
        });
    }

    private void ConfigureDashboard(
        string ownerName = "Bà Tám Bún Bò",
        OwnerDashboardWorkspaceDto? workspace = null)
    {
        Services.AddSingleton<AuthenticationStateProvider>(
            new TestAuthenticationStateProvider(ownerName));
        Services.AddSingleton<IOwnerPortalService>(
            new TestOwnerPortalService(workspace ?? BuildWorkspace()));
    }

    private static OwnerDashboardWorkspaceDto BuildWorkspace() => new()
    {
        Summary = new OwnerWorkspaceSummaryDto
        {
            TotalPois = 4,
            PublishedPois = 2,
            PendingReviewPois = 1,
            ReadyAudioAssets = 12
        },
        PublishedRows =
        [
            new OwnerDashboardPublishedRowDto
            {
                PoiId = 1,
                PoiName = "Bún mắm Vĩnh Khánh",
                CategoryName = "Bún",
                ListenCount = 128,
                Trend = [3, 6, 4, 8, 7, 9, 10],
                LocationHint = "Vĩnh Khánh"
            },
            new OwnerDashboardPublishedRowDto
            {
                PoiId = 2,
                PoiName = "Cơm tấm than hồng",
                CategoryName = "Cơm",
                ListenCount = 74,
                Trend = [2, 3, 5, 5, 6, 7, 8],
                LocationHint = "Khánh Hội"
            }
        ],
        RecentActivities =
        [
            new OwnerDashboardRecentActivityDto
            {
                Type = "moderation",
                Title = "Admin yêu cầu bổ sung nội dung nguồn",
                Description = "POI Bún mắm Vĩnh Khánh cần cập nhật nguồn trước khi duyệt.",
                OccurredAtUtc = DateTime.UtcNow.AddHours(-2),
                Tone = "warn",
                LinkedPoiId = 1
            },
            new OwnerDashboardRecentActivityDto
            {
                Type = "audio",
                Title = "Audio nguồn tiếng Việt đã sẵn sàng",
                Description = "Cơm tấm than hồng đã có audio nguồn để owner kiểm tra.",
                OccurredAtUtc = DateTime.UtcNow.AddHours(-6),
                Tone = "good",
                LinkedPoiId = 2
            }
        ]
    };

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

    private sealed class TestOwnerPortalService(OwnerDashboardWorkspaceDto workspace) : IOwnerPortalService
    {
        public Task<OwnerDashboardWorkspaceDto> GetDashboardWorkspaceAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(workspace);

        public Task<OwnerShellSummaryDto> GetShellSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerShellSummaryDto
            {
                TotalPois = workspace.Summary.TotalPois,
                PublishedPois = workspace.Summary.PublishedPois,
                PendingModerationRequests = workspace.Summary.PendingReviewPois,
                UnreadNotifications = 0
            });

        public Task<OwnerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OwnerDashboardDto
            {
                TotalPois = workspace.Summary.TotalPois,
                PublishedPois = workspace.Summary.PublishedPois,
                DraftPois = 1,
                PendingReviewPois = workspace.Summary.PendingReviewPois,
                TotalAudioAssets = workspace.Summary.ReadyAudioAssets,
                PendingModerationRequests = workspace.RecentActivities.Count(activity => activity.Type == "moderation"),
                UnreadNotifications = 0
            });

        public Task<IReadOnlyList<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PoiDto>>(
                workspace.PublishedRows.Select(row => new PoiDto
                {
                    Id = row.PoiId,
                    Name = row.PoiName,
                    Slug = row.PoiName.ToLowerInvariant().Replace(' ', '-'),
                    CategoryName = row.CategoryName,
                    Priority = row.PoiId,
                    Status = NarrationApp.Shared.Enums.PoiStatus.Published
                }).ToArray());

        public Task<PoiDto> GetPoiAsync(int poiId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

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
