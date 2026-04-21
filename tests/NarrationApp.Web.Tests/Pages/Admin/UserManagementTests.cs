using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Pages.Admin;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Pages.Admin;

public sealed class UserManagementTests : TestContext
{
    [Fact]
    public void Filters_user_manager_to_owner_and_tourist_without_role_controls()
    {
        var service = new TestAdminPortalService();
        Services.AddSingleton<IAdminPortalService>(service);

        var cut = RenderComponent<UserManagement>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find(".user-surface"));
            Assert.Contains("User Manager", cut.Markup);
            Assert.Contains("Quản lý người dùng", cut.Markup);
            Assert.Contains("Tổng users", cut.Markup);
            Assert.Contains("Online", cut.Markup);
            Assert.Contains("POI Owners", cut.Markup);
            Assert.Contains("Tourists", cut.Markup);
            Assert.DoesNotContain("admin@narration.app", cut.Markup);
            Assert.Contains("owner@narration.app", cut.Markup);
            Assert.Contains("visitor@narration.app", cut.Markup);
            Assert.DoesNotContain("Vai trò", cut.Markup);
            Assert.DoesNotContain("Lưu role", cut.Markup);
            Assert.DoesNotContain("Trung tâm quyền truy cập", cut.Markup);
        });

        cut.Find("button[data-action='filter-user-owner']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("owner@narration.app", cut.Markup);
            Assert.DoesNotContain("visitor@narration.app", cut.Markup);
        });

        cut.Find("button[data-action='filter-user-tourist']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("owner@narration.app", cut.Markup);
            Assert.Contains("visitor@narration.app", cut.Markup);
        });
    }

    private sealed class TestAdminPortalService : IAdminPortalService
    {
        private readonly List<UserSummaryDto> _users =
        [
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "visitor@narration.app",
                PreferredLanguage = "vi",
                IsActive = true,
                RoleName = "tourist",
                DeviceCount = 2,
                ActiveDeviceCount = 1,
                IsOnline = true,
                LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "owner@narration.app",
                PreferredLanguage = "en",
                IsActive = true,
                RoleName = "poi_owner",
                DeviceCount = 1,
                ActiveDeviceCount = 0,
                IsOnline = false,
                LastSeenAtUtc = DateTime.UtcNow.AddHours(-4)
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "admin@narration.app",
                PreferredLanguage = "vi",
                IsActive = true,
                RoleName = "admin",
                DeviceCount = 1,
                ActiveDeviceCount = 1,
                IsOnline = true,
                LastSeenAtUtc = DateTime.UtcNow.AddMinutes(-1)
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
            return Task.FromResult<IReadOnlyList<UserSummaryDto>>(_users.ToArray());
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
