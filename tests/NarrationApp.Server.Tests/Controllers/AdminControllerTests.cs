using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using NarrationApp.Server.Controllers;
using NarrationApp.Server.Data;
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
    private static AdminController CreateController(
        AppDbContext dbContext,
        IQrWebPresenceTracker? qrWebPresenceTracker = null,
        IVisitorMobilePresenceTracker? visitorMobilePresenceTracker = null)
    {
        return new AdminController(
            new StubModerationService(),
            new StubAnalyticsService(),
            dbContext,
            new VisitorDeviceDashboardService(
                dbContext,
                qrWebPresenceTracker ?? new StubQrWebPresenceTracker(),
                visitorMobilePresenceTracker ?? new StubVisitorMobilePresenceTracker()),
            new PoiReviewService(dbContext));
    }

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

        var controller = CreateController(dbContext);

        var actionResult = await controller.PoisAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<AdminPoiDto>>>(okResult.Value);
        var poi = Assert.Single(response.Data!.Where(item => item.Id == 1));

        Assert.Equal(PoiStatus.Published, poi.Status);
        Assert.Null(poi.PendingModerationId);
    }

    [Fact]
    public async Task UpdatePoiPriorityAsync_updates_priority_and_returns_admin_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.AsNoTracking().FirstAsync();
        var controller = CreateController(dbContext);

        var actionResult = await controller.UpdatePoiPriorityAsync(
            poi.Id,
            new UpdatePoiPriorityRequest { Priority = poi.Priority + 25 },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<AdminPoiDto>>(okResult.Value);
        var persistedPriority = await dbContext.Pois
            .Where(item => item.Id == poi.Id)
            .Select(item => item.Priority)
            .SingleAsync();

        Assert.Equal(poi.Priority + 25, response.Data!.Priority);
        Assert.Equal(poi.Priority + 25, persistedPriority);
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

        var controller = CreateController(dbContext);

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

        var controller = CreateController(dbContext);

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
    public async Task VisitorDevicesAsync_marks_qr_web_device_offline_after_five_seconds_without_presence()
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

        var controller = CreateController(dbContext);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data!);
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
        presenceTracker.Track("qr-web-heartbeat-001", now.AddSeconds(-3));

        var controller = CreateController(dbContext, qrWebPresenceTracker: presenceTracker);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data!);
        Assert.Equal("qr-web-heartbeat-001", visitor.DeviceId);
        Assert.True(visitor.IsOnline);
        Assert.NotNull(visitor.LastSeenAtUtc);
        Assert.True(visitor.LastSeenAtUtc >= now.AddSeconds(-5));
    }

    [Fact]
    public async Task VisitorDevicesAsync_includes_presence_only_mobile_device_as_online_guest()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var now = DateTime.UtcNow;
        var mobilePresenceTracker = new StubVisitorMobilePresenceTracker();
        mobilePresenceTracker.Track("android-emulator-5554", "mobile-presence", "vi-VN", now.AddSeconds(-3));

        var controller = CreateController(dbContext, visitorMobilePresenceTracker: mobilePresenceTracker);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data!);
        Assert.Equal("android-emulator-5554", visitor.DeviceId);
        Assert.Equal("guest", visitor.RoleName);
        Assert.True(visitor.IsOnline);
        Assert.Equal(0, visitor.TrackingCount);
        Assert.Equal(0, visitor.VisitCount);
        Assert.Equal(0, visitor.TriggerCount);
        Assert.Equal("vi-VN", visitor.PreferredLanguage);
    }

    [Fact]
    public async Task VisitorDevicesAsync_includes_presence_only_qr_web_device_as_online_guest()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var now = DateTime.UtcNow;
        var qrWebPresenceTracker = new StubQrWebPresenceTracker();
        qrWebPresenceTracker.Track("qr-web-poi-list-001", now.AddSeconds(-3));

        var controller = CreateController(dbContext, qrWebPresenceTracker: qrWebPresenceTracker);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        var visitor = Assert.Single(response.Data!);
        Assert.Equal("qr-web-poi-list-001", visitor.DeviceId);
        Assert.Equal("guest", visitor.RoleName);
        Assert.True(visitor.IsOnline);
        Assert.False(visitor.AutoPlayEnabled);
        Assert.False(visitor.BackgroundTrackingEnabled);
        Assert.Equal(0, visitor.TrackingCount);
        Assert.Equal(0, visitor.VisitCount);
        Assert.Equal(0, visitor.TriggerCount);
        Assert.Equal("vi-VN", visitor.PreferredLanguage);
    }

    [Fact]
    public async Task ExportEventLogCsvAsync_returns_structured_visit_event_log_file()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.OrderBy(item => item.Id).FirstAsync();
        poi.Name = "Ốc Oanh, Vĩnh Khánh";
        dbContext.VisitEvents.Add(new VisitEvent
        {
            DeviceId = "demo-device-001",
            PoiId = poi.Id,
            EventType = EventType.AudioPlay,
            Source = "mobile-app",
            ListenDurationSeconds = 87,
            Lat = 10.7609,
            Lng = 106.7054,
            CreatedAt = new DateTime(2026, 5, 9, 9, 30, 0, DateTimeKind.Utc)
        });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.ExportEventLogCsvAsync(CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.StartsWith("visit-event-log-", file.FileDownloadName, StringComparison.Ordinal);
        Assert.EndsWith(".csv", file.FileDownloadName, StringComparison.Ordinal);
        Assert.Contains("event_id,created_at_utc,event_type,source,device_id,user_id,user_email,poi_id,poi_name,lat,lng,listen_duration_seconds", csv);
        Assert.Contains("\"AudioPlay\"", csv);
        Assert.Contains("\"mobile-app\"", csv);
        Assert.Contains("\"demo-device-001\"", csv);
        Assert.Contains("\"Ốc Oanh, Vĩnh Khánh\"", csv);
        Assert.Contains("\"10.7609\"", csv);
        Assert.Contains("\"106.7054\"", csv);
        Assert.Contains("\"87\"", csv);
    }

    [Fact]
    public async Task VisitorDevicesAsync_omits_presence_only_mobile_device_after_short_presence_timeout()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var now = DateTime.UtcNow;
        var mobilePresenceTracker = new StubVisitorMobilePresenceTracker();
        mobilePresenceTracker.Track("android-emulator-stale", "mobile-presence", "vi-VN", now.AddSeconds(-6));

        var controller = CreateController(dbContext, visitorMobilePresenceTracker: mobilePresenceTracker);

        var actionResult = await controller.VisitorDevicesAsync(CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<VisitorDeviceSummaryDto>>>(okResult.Value);

        Assert.DoesNotContain(response.Data!, item => item.DeviceId == "android-emulator-stale");
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

        public IReadOnlyCollection<QrWebPresenceSnapshot> GetAll()
        {
            return _lastSeenByDeviceId
                .Select(item => new QrWebPresenceSnapshot(item.Key, item.Value))
                .ToArray();
        }

        public void Track(string deviceId, DateTime? seenAtUtc = null)
        {
            _lastSeenByDeviceId[deviceId] = seenAtUtc ?? DateTime.UtcNow;
        }
    }

    private sealed class StubVisitorMobilePresenceTracker : IVisitorMobilePresenceTracker
    {
        private readonly Dictionary<string, VisitorMobilePresenceSnapshot> _presenceByDeviceId = new(StringComparer.OrdinalIgnoreCase);

        public VisitorMobilePresenceSnapshot? Get(string deviceId)
        {
            return _presenceByDeviceId.TryGetValue(deviceId, out var value) ? value : null;
        }

        public IReadOnlyCollection<VisitorMobilePresenceSnapshot> GetAll()
        {
            return _presenceByDeviceId.Values.ToArray();
        }

        public void MarkOffline(string deviceId)
        {
            _presenceByDeviceId.Remove(deviceId);
        }

        public void Track(string deviceId, string source, string? preferredLanguage, DateTime? seenAtUtc = null)
        {
            _presenceByDeviceId[deviceId] = new VisitorMobilePresenceSnapshot(
                deviceId,
                source,
                preferredLanguage ?? string.Empty,
                seenAtUtc ?? DateTime.UtcNow);
        }
    }
}
