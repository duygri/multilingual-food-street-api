using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed partial class VisitorDeviceDashboardService(
    AppDbContext dbContext,
    IQrWebPresenceTracker qrWebPresenceTracker,
    IVisitorMobilePresenceTracker visitorMobilePresenceTracker) : IVisitorDeviceDashboardService
{
    private static readonly TimeSpan QrWebOnlineWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MobilePresenceOnlineWindow = TimeSpan.FromSeconds(5);

    public async Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var onlineThresholdUtc = nowUtc.AddMinutes(-15);
        var qrWebPresenceByDeviceId = qrWebPresenceTracker.GetAll()
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .GroupBy(item => item.DeviceId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.LastSeenAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);
        var mobilePresenceByDeviceId = visitorMobilePresenceTracker.GetAll()
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .GroupBy(item => item.DeviceId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.LastSeenAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);
        var touristUsersById = await dbContext.AppUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role != null && user.Role.Name == "tourist")
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var visitEventActivity = await dbContext.VisitEvents
            .AsNoTracking()
            .Select(item => new VisitEventActivity(
                item.UserId,
                item.DeviceId,
                item.PoiId,
                item.EventType,
                item.Source,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return visitEventActivity
            .Where(item => !string.IsNullOrWhiteSpace(item.DeviceId))
            .GroupBy(
                item => $"{(item.UserId.HasValue ? item.UserId.Value.ToString("N") : "guest")}|{item.DeviceId.Trim().ToLowerInvariant()}",
                StringComparer.Ordinal)
            .Select(group => BuildFromVisitGroup(group, touristUsersById, qrWebPresenceByDeviceId, mobilePresenceByDeviceId, nowUtc, onlineThresholdUtc))
            .Where(item => item is not null)
            .Cast<VisitorDeviceSummaryDto>()
            .Concat(BuildPresenceOnlyQrWebVisitorDevices(qrWebPresenceByDeviceId, nowUtc, onlineThresholdUtc))
            .Concat(BuildPresenceOnlyVisitorDevices(mobilePresenceByDeviceId, nowUtc, onlineThresholdUtc))
            .GroupBy(item => item.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.IsOnline)
                .ThenByDescending(item => item.LastSeenAtUtc)
                .ThenByDescending(item => item.TrackingCount)
                .First())
            .OrderByDescending(item => item.IsOnline)
            .ThenByDescending(item => item.LastSeenAtUtc)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private VisitorDeviceSummaryDto? BuildFromVisitGroup(
        IGrouping<string, VisitEventActivity> group,
        IReadOnlyDictionary<Guid, AppUser> touristUsersById,
        IReadOnlyDictionary<string, QrWebPresenceSnapshot> qrWebPresenceByDeviceId,
        IReadOnlyDictionary<string, VisitorMobilePresenceSnapshot> mobilePresenceByDeviceId,
        DateTime nowUtc,
        DateTime onlineThresholdUtc)
    {
        var ordered = group
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();
        var latest = ordered[0];
        var deviceId = latest.DeviceId.Trim();
        mobilePresenceByDeviceId.TryGetValue(deviceId, out var mobilePresence);
        AppUser? tourist = null;
        var roleName = "guest";

        if (latest.UserId.HasValue)
        {
            if (!touristUsersById.TryGetValue(latest.UserId.Value, out tourist))
            {
                return null;
            }

            roleName = "tourist";
        }

        var effectiveSource = ResolveEffectiveVisitorSource(latest.Source, latest.CreatedAt, mobilePresence);
        var normalizedLanguage = NormalizeLanguageTag(tourist?.PreferredLanguage);
        if (string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            normalizedLanguage = NormalizeLanguageTag(mobilePresence?.PreferredLanguage);
        }

        if (string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            normalizedLanguage = InferLanguageTag(effectiveSource, deviceId);
        }

        var passiveVisitorDevice = IsPassiveVisitorDevice(effectiveSource, deviceId);
        DateTime? qrWebPresenceLastSeenUtc = IsQrWebVisitor(effectiveSource, deviceId)
            ? qrWebPresenceByDeviceId.TryGetValue(deviceId, out var qrPresence) ? qrPresence.LastSeenAtUtc : null
            : null;
        var effectiveLastSeenAtUtc = GetEffectiveVisitorLastSeenUtc(
            latest.CreatedAt,
            qrWebPresenceLastSeenUtc,
            mobilePresence?.LastSeenAtUtc);

        return new VisitorDeviceSummaryDto
        {
            Id = CreateStableVisitorId(latest.UserId, deviceId),
            DisplayName = FormatVisitorDisplayName(deviceId, effectiveSource, roleName, tourist?.FullName),
            AccountLabel = roleName == "tourist" ? tourist?.Email ?? string.Empty : string.Empty,
            DeviceId = deviceId,
            PreferredLanguage = normalizedLanguage,
            RoleName = roleName,
            IsOnline = IsVisitorOnline(effectiveSource, deviceId, effectiveLastSeenAtUtc, onlineThresholdUtc, nowUtc),
            AutoPlayEnabled = !passiveVisitorDevice,
            BackgroundTrackingEnabled = !passiveVisitorDevice,
            TrackingCount = ordered.Length,
            VisitCount = ordered.Select(item => item.PoiId).Distinct().Count(),
            TriggerCount = ordered.Count(item => item.EventType == EventType.GeofenceEnter),
            LastSeenAtUtc = effectiveLastSeenAtUtc
        };
    }

    private IReadOnlyList<VisitorDeviceSummaryDto> BuildPresenceOnlyQrWebVisitorDevices(
        IReadOnlyDictionary<string, QrWebPresenceSnapshot> qrWebPresenceByDeviceId,
        DateTime nowUtc,
        DateTime onlineThresholdUtc)
    {
        return qrWebPresenceByDeviceId.Values
            .Where(item => IsVisitorOnline("qr-web", item.DeviceId, item.LastSeenAtUtc, onlineThresholdUtc, nowUtc))
            .Select(item => new VisitorDeviceSummaryDto
            {
                Id = CreateStableVisitorId(null, item.DeviceId),
                DisplayName = FormatVisitorDisplayName(item.DeviceId, "qr-web", "guest", null),
                AccountLabel = string.Empty,
                DeviceId = item.DeviceId,
                PreferredLanguage = InferLanguageTag("qr-web", item.DeviceId),
                RoleName = "guest",
                IsOnline = true,
                AutoPlayEnabled = false,
                BackgroundTrackingEnabled = false,
                TrackingCount = 0,
                VisitCount = 0,
                TriggerCount = 0,
                LastSeenAtUtc = item.LastSeenAtUtc
            })
            .ToArray();
    }

    private IReadOnlyList<VisitorDeviceSummaryDto> BuildPresenceOnlyVisitorDevices(
        IReadOnlyDictionary<string, VisitorMobilePresenceSnapshot> mobilePresenceByDeviceId,
        DateTime nowUtc,
        DateTime onlineThresholdUtc)
    {
        return mobilePresenceByDeviceId.Values
            .Where(item => IsVisitorOnline(item.Source, item.DeviceId, item.LastSeenAtUtc, onlineThresholdUtc, nowUtc))
            .Select(item =>
            {
                var normalizedLanguage = NormalizeLanguageTag(item.PreferredLanguage);
                if (string.IsNullOrWhiteSpace(normalizedLanguage))
                {
                    normalizedLanguage = InferLanguageTag(item.Source, item.DeviceId);
                }

                return new VisitorDeviceSummaryDto
                {
                    Id = CreateStableVisitorId(null, item.DeviceId),
                    DisplayName = FormatVisitorDisplayName(item.DeviceId, item.Source, "guest", null),
                    AccountLabel = string.Empty,
                    DeviceId = item.DeviceId,
                    PreferredLanguage = normalizedLanguage,
                    RoleName = "guest",
                    IsOnline = true,
                    AutoPlayEnabled = true,
                    BackgroundTrackingEnabled = true,
                    TrackingCount = 0,
                    VisitCount = 0,
                    TriggerCount = 0,
                    LastSeenAtUtc = item.LastSeenAtUtc
                };
            })
            .ToArray();
    }

}
