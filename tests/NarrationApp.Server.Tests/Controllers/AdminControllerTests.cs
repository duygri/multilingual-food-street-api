using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Controllers;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Controllers;

public sealed class AdminControllerTests
{
    [Fact]
    public async Task UsersAsync_returns_device_and_presence_metrics()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-metrics@narration.app");
        var poiId = await dbContext.Pois.Select(item => item.Id).FirstAsync();
        var now = DateTime.UtcNow;

        dbContext.VisitEvents.AddRange(
            new VisitEvent
            {
                UserId = tourist.Id,
                DeviceId = "device-alpha",
                PoiId = poiId,
                EventType = EventType.AudioPlay,
                Source = "mobile-app",
                CreatedAt = now.AddMinutes(-3)
            },
            new VisitEvent
            {
                UserId = tourist.Id,
                DeviceId = "device-beta",
                PoiId = poiId,
                EventType = EventType.QrScan,
                Source = "mobile-app",
                CreatedAt = now.AddHours(-3)
            },
            new VisitEvent
            {
                UserId = tourist.Id,
                DeviceId = "device-alpha",
                PoiId = poiId,
                EventType = EventType.GeofenceEnter,
                Source = "mobile-app",
                CreatedAt = now.AddMinutes(-1)
            });

        await dbContext.SaveChangesAsync();

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext);

        var actionResult = await controller.UsersAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<UserSummaryDto>>>(okResult.Value);
        var summary = Assert.Single(response.Data!.Where(item => item.Email == tourist.Email));
        var admin = Assert.Single(response.Data!.Where(item => item.Email == "admin@narration.app"));

        Assert.Equal(2, summary.DeviceCount);
        Assert.Equal(1, summary.ActiveDeviceCount);
        Assert.True(summary.IsOnline);
        Assert.NotNull(summary.LastSeenAtUtc);

        Assert.Equal(0, admin.DeviceCount);
        Assert.Equal(0, admin.ActiveDeviceCount);
        Assert.False(admin.IsOnline);
        Assert.Null(admin.LastSeenAtUtc);
    }

    private sealed class StubModerationService : IModerationService
    {
        public Task<ModerationRequestDto> CreateAsync(Guid requestedBy, CreateModerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ModerationRequestDto>> GetByRequesterAsync(Guid requestedBy, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ModerationRequestDto> ReviewAsync(int requestId, Guid reviewedBy, bool approved, string? reviewNote, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubAnalyticsService : IAnalyticsService
    {
        public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DashboardDto());
        }

        public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<TopPoiDto>> GetTopPoisAsync(int take = 10, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PoiAnalyticsDto> GetPoiAnalyticsAsync(int poiId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<AudioPlayAnalyticsDto> GetAudioPlayAnalyticsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
