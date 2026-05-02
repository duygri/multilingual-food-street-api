using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class UserManagementTests : TestContext
{
    [Fact]
    public void User_management_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Web", "Pages", "Admin");
        var markupPath = Path.Combine(pageRoot, "UserManagement.razor");
        var expectedPartials = new[]
        {
            ("UserManagement.razor.cs", "OnInitializedAsync"),
            ("UserManagement.Actions.razor.cs", "RunRefreshLoopAsync"),
            ("UserManagement.Presentation.razor.cs", "GetLastSeenRelative")
        };

        var markup = File.ReadAllText(markupPath);
        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("partial class UserManagement", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "UserManagement.razor.cs")).Length <= 60);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "UserManagement.Actions.razor.cs")).Length <= 110);
        Assert.True(File.ReadAllLines(Path.Combine(pageRoot, "UserManagement.Presentation.razor.cs")).Length <= 60);
    }

    [Fact]
    public void Renders_only_online_visitor_devices_and_hides_total_metric()
    {
        var service = new TestAdminPortalService();
        Services.AddSingleton<IAdminPortalService>(service);

        var cut = RenderComponent<UserManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".visitor-page"));
            Assert.Contains("Thiết bị visitor", cut.Markup);
            Assert.Contains("Đang online", cut.Markup);
            Assert.Contains("android-pixel-7-tourist001", cut.Markup);
            Assert.Contains("Android Pixel 7", cut.Markup);
            Assert.Contains("vi-VN", cut.Markup);
            Assert.Contains("Thiết bị", cut.Markup);
            Assert.Contains("Visit", cut.Markup);
            Assert.Contains("Trigger", cut.Markup);
            Assert.DoesNotContain("Tổng visitor", cut.Markup);
            Assert.DoesNotContain("android-pixel-7-guest001", cut.Markup);
            Assert.DoesNotContain("Visitor quét QR", cut.Markup);
            Assert.DoesNotContain("en-US", cut.Markup);
            Assert.DoesNotContain("Offline", cut.Markup);
            Assert.DoesNotContain("owner@narration.app", cut.Markup);
            Assert.DoesNotContain("visitor@narration.app", cut.Markup);
            Assert.DoesNotContain("Không hoạt động", cut.Markup);
            Assert.DoesNotContain("Auto play", cut.Markup);
            Assert.DoesNotContain("Background", cut.Markup);
        });

        Assert.Single(cut.FindAll("button[data-action='refresh-visitor-data']"));
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Empty(cut.FindAll("button[data-action='visitor-row-edit']"));
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        private readonly List<VisitorDeviceSummaryDto> _visitorDevices =
        [
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DisplayName = "Android Pixel 7",
                AccountLabel = "visitor@narration.app",
                DeviceId = "android-pixel-7-tourist001",
                PreferredLanguage = "vi-VN",
                RoleName = "tourist",
                IsOnline = true,
                AutoPlayEnabled = true,
                BackgroundTrackingEnabled = true,
                TrackingCount = 12,
                VisitCount = 4,
                TriggerCount = 2,
                LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DisplayName = "Visitor quét QR",
                AccountLabel = string.Empty,
                DeviceId = "android-pixel-7-guest001",
                PreferredLanguage = "en-US",
                RoleName = "guest",
                IsOnline = false,
                AutoPlayEnabled = false,
                BackgroundTrackingEnabled = false,
                TrackingCount = 3,
                VisitCount = 1,
                TriggerCount = 0,
                LastSeenAtUtc = DateTime.UtcNow.AddHours(-2)
            }
        ];

        public Task<DashboardDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto());
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
            return Task.FromResult<IReadOnlyList<VisitorDeviceSummaryDto>>(_visitorDevices.ToArray());
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
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateUserRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
