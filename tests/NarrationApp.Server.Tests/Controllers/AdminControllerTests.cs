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
    public async Task PoisAsync_does_not_expose_pending_moderation_for_published_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var owner = await dbContext.AppUsers.SingleAsync(user => user.Email == "owner@narration.app");

        dbContext.ModerationRequests.Add(new ModerationRequest
        {
            EntityType = "poi",
            EntityId = "1",
            Status = ModerationStatus.Pending,
            RequestedBy = owner.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await dbContext.SaveChangesAsync();

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext, new StubQrWebPresenceTracker());

        var actionResult = await controller.PoisAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<AdminPoiDto>>>(okResult.Value);
        var poi = Assert.Single(response.Data!.Where(item => item.Id == 1));

        Assert.Equal(PoiStatus.Published, poi.Status);
        Assert.Null(poi.PendingModerationId);
    }

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
            },
            new VisitEvent
            {
                UserId = null,
                DeviceId = "android-pixel-7-guest001",
                PoiId = poiId,
                EventType = EventType.AudioPlay,
                Source = "guest-mode",
                CreatedAt = now.AddMinutes(-5)
            },
            new VisitEvent
            {
                UserId = null,
                DeviceId = "android-pixel-7-guest001",
                PoiId = poiId,
                EventType = EventType.QrScan,
                Source = "guest-mode",
                CreatedAt = now.AddMinutes(-2)
            });

        await dbContext.SaveChangesAsync();

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext, new StubQrWebPresenceTracker());

        var actionResult = await controller.UsersAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<UserSummaryDto>>>(okResult.Value);
        var summary = Assert.Single(response.Data!.Where(item => item.Email == tourist.Email));
        var admin = Assert.Single(response.Data!.Where(item => item.Email == "admin@narration.app"));
        var guest = Assert.Single(response.Data!.Where(item => item.RoleName == "guest"));

        Assert.Equal(2, summary.DeviceCount);
        Assert.Equal(1, summary.ActiveDeviceCount);
        Assert.True(summary.IsOnline);
        Assert.NotNull(summary.LastSeenAtUtc);
        Assert.Equal("android-pixel-7-guest001", guest.DeviceId);
        Assert.Contains("Android", guest.DisplayName, StringComparison.Ordinal);
        Assert.Equal(1, guest.DeviceCount);
        Assert.Equal(1, guest.ActiveDeviceCount);
        Assert.True(guest.IsOnline);
        Assert.NotNull(guest.LastSeenAtUtc);

        Assert.Equal(0, admin.DeviceCount);
        Assert.Equal(0, admin.ActiveDeviceCount);
        Assert.False(admin.IsOnline);
        Assert.Null(admin.LastSeenAtUtc);
    }

    [Fact]
    public async Task VisitorDevicesAsync_returns_tourist_and_guest_devices_without_owner_rows()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "visitor@narration.app");
        var owner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner-device@narration.app");
        var poiId = await dbContext.Pois.Select(item => item.Id).FirstAsync();
        var now = DateTime.UtcNow;

        dbContext.VisitEvents.AddRange(
            new VisitEvent
            {
                UserId = tourist.Id,
                DeviceId = "android-pixel-7-tourist001",
                PoiId = poiId,
                EventType = EventType.AudioPlay,
                Source = "mobile-app",
                CreatedAt = now.AddMinutes(-2)
            },
            new VisitEvent
            {
                UserId = tourist.Id,
                DeviceId = "android-pixel-7-tourist001",
                PoiId = poiId,
                EventType = EventType.GeofenceEnter,
                Source = "mobile-app",
                CreatedAt = now.AddMinutes(-1)
            },
            new VisitEvent
            {
                UserId = null,
                DeviceId = "qr-web-b35ca655340b",
                PoiId = poiId,
                EventType = EventType.QrScan,
                Source = "qr-web",
                CreatedAt = now.AddHours(-1)
            },
            new VisitEvent
            {
                UserId = owner.Id,
                DeviceId = "owner-ios-001",
                PoiId = poiId,
                EventType = EventType.AudioPlay,
                Source = "owner-app",
                CreatedAt = now.AddMinutes(-4)
            });

        await dbContext.SaveChangesAsync();

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext, new StubQrWebPresenceTracker());

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        Assert.Equal(2, response.Data!.Count);

        var touristDevice = Assert.Single(response.Data.Where(item => item.RoleName == "tourist"));
        var guestDevice = Assert.Single(response.Data.Where(item => item.RoleName == "guest"));

        Assert.Equal("visitor@narration.app", touristDevice.AccountLabel);
        Assert.Equal("android-pixel-7-tourist001", touristDevice.DeviceId);
        Assert.Equal("vi-VN", touristDevice.PreferredLanguage);
        Assert.True(touristDevice.IsOnline);
        Assert.True(touristDevice.AutoPlayEnabled);
        Assert.True(touristDevice.BackgroundTrackingEnabled);
        Assert.Equal(2, touristDevice.TrackingCount);
        Assert.Equal(1, touristDevice.VisitCount);
        Assert.Equal(1, touristDevice.TriggerCount);

        Assert.Equal("qr-web-b35ca655340b", guestDevice.DeviceId);
        Assert.Equal("guest", guestDevice.RoleName);
        Assert.False(guestDevice.IsOnline);
        Assert.False(guestDevice.AutoPlayEnabled);
        Assert.False(guestDevice.BackgroundTrackingEnabled);
        Assert.Equal(1, guestDevice.TrackingCount);
        Assert.Equal(1, guestDevice.VisitCount);
        Assert.Equal(0, guestDevice.TriggerCount);
        Assert.DoesNotContain(response.Data, item => string.Equals(item.AccountLabel, owner.Email, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VisitorDevicesAsync_marks_qr_web_device_offline_after_thirty_seconds_without_presence()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poiId = await dbContext.Pois.Select(item => item.Id).FirstAsync();
        var now = DateTime.UtcNow;

        dbContext.VisitEvents.Add(new VisitEvent
        {
            UserId = null,
            DeviceId = "qr-web-timeout-001",
            PoiId = poiId,
            EventType = EventType.QrScan,
            Source = "qr-web",
            CreatedAt = now.AddMinutes(-1)
        });

        await dbContext.SaveChangesAsync();

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext, new StubQrWebPresenceTracker());

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data);
        Assert.Equal("qr-web-timeout-001", visitor.DeviceId);
        Assert.False(visitor.IsOnline);
    }

    [Fact]
    public async Task VisitorDevicesAsync_marks_qr_web_device_online_when_presence_is_recent()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poiId = await dbContext.Pois.Select(item => item.Id).FirstAsync();
        var now = DateTime.UtcNow;

        dbContext.VisitEvents.Add(new VisitEvent
        {
            UserId = null,
            DeviceId = "qr-web-heartbeat-001",
            PoiId = poiId,
            EventType = EventType.QrScan,
            Source = "qr-web",
            CreatedAt = now.AddMinutes(-1)
        });

        await dbContext.SaveChangesAsync();

        var presenceTracker = new StubQrWebPresenceTracker();
        presenceTracker.Track("qr-web-heartbeat-001", now.AddSeconds(-5));

        var controller = new AdminController(new StubModerationService(), new StubAnalyticsService(), dbContext, presenceTracker);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data);
        Assert.Equal("qr-web-heartbeat-001", visitor.DeviceId);
        Assert.True(visitor.IsOnline);
        Assert.NotNull(visitor.LastSeenAtUtc);
        Assert.True(visitor.LastSeenAtUtc >= now.AddSeconds(-10));
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

    private sealed class StubQrWebPresenceTracker : IQrWebPresenceTracker
    {
        private readonly Dictionary<string, DateTime> _lastSeenByDeviceId = new(StringComparer.OrdinalIgnoreCase);

        public DateTime? GetLastSeenUtc(string deviceId)
        {
            return _lastSeenByDeviceId.TryGetValue(deviceId, out var value) ? value : null;
        }

        public void Track(string deviceId, DateTime? seenAtUtc = null)
        {
            _lastSeenByDeviceId[deviceId] = seenAtUtc ?? DateTime.UtcNow;
        }
    }
}
