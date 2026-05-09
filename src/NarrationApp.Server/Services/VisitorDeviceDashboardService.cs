using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed class VisitorDeviceDashboardService(
    AppDbContext dbContext,
    IQrWebPresenceTracker qrWebPresenceTracker,
    IVisitorMobilePresenceTracker visitorMobilePresenceTracker) : IVisitorDeviceDashboardService
{
    private static readonly TimeSpan QrWebOnlineWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MobilePresenceOnlineWindow = TimeSpan.FromSeconds(10);

    public async Task<IReadOnlyList<VisitorDeviceSummaryDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var onlineThresholdUtc = nowUtc.AddMinutes(-15);
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
            .Select(group => BuildFromVisitGroup(group, touristUsersById, mobilePresenceByDeviceId, nowUtc, onlineThresholdUtc))
            .Where(item => item is not null)
            .Cast<VisitorDeviceSummaryDto>()
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
        var qrWebPresenceLastSeenUtc = IsQrWebVisitor(effectiveSource, deviceId)
            ? qrWebPresenceTracker.GetLastSeenUtc(deviceId)
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

    private static Guid CreateStableVisitorId(Guid? userId, string deviceId)
    {
        var seed = $"{(userId.HasValue ? userId.Value.ToString("N") : "guest")}|{deviceId.Trim().ToLowerInvariant()}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static string FormatVisitorDisplayName(string deviceId, string? source, string roleName, string? fallbackName)
    {
        if (IsQrVisitor(source, deviceId))
        {
            return $"{InferDevicePlatform(deviceId, source)} quét QR";
        }

        var deviceLabel = FormatReadableDeviceName(deviceId);
        if (!string.Equals(deviceLabel, "Thiết bị visitor", StringComparison.OrdinalIgnoreCase))
        {
            return deviceLabel;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName.Trim();
        }

        return string.Equals(roleName, "guest", StringComparison.OrdinalIgnoreCase)
            ? "Khách ẩn danh"
            : "Visitor mobile";
    }

    private static string FormatReadableDeviceName(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "Thiết bị visitor";
        }

        if (string.Equals(deviceId, "anonymous-device", StringComparison.OrdinalIgnoreCase))
        {
            return "Khách ẩn danh";
        }

        var tokens = deviceId
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !IsVisitorNoiseToken(token))
            .ToArray();

        if (tokens.Length == 0)
        {
            return $"Thiết bị visitor {GetDeviceSuffix(deviceId)}".Trim();
        }

        var label = string.Join(" ", tokens.Select(MapDeviceToken).Take(4));
        return $"{label} {GetDeviceSuffix(deviceId)}".Trim();
    }

    private static bool IsVisitorNoiseToken(string token)
    {
        var normalized = token.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return true;
        }

        return normalized is "device" or "devices" or "guest" or "tourist" or "mobile" or "mode" or "app" or "scan" or "qr" or "web"
            || normalized.StartsWith("guest", StringComparison.Ordinal)
            || normalized.StartsWith("tourist", StringComparison.Ordinal)
            || normalized.StartsWith("device", StringComparison.Ordinal)
            || LooksLikeOpaqueToken(token);
    }

    private static bool IsPassiveVisitorDevice(string? source, string deviceId)
    {
        if (IsQrVisitor(source, deviceId))
        {
            return true;
        }

        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("web", StringComparison.Ordinal)
            || combined.Contains("browser", StringComparison.Ordinal);
    }

    private static bool IsQrVisitor(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("qr", StringComparison.Ordinal);
    }

    private static bool IsQrWebVisitor(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();
        return combined.Contains("qr-web", StringComparison.Ordinal)
            || combined.Contains("browser", StringComparison.Ordinal);
    }

    private static string? ResolveEffectiveVisitorSource(string? latestEventSource, DateTime latestEventAtUtc, VisitorMobilePresenceSnapshot? mobilePresence)
    {
        if (mobilePresence is null || mobilePresence.LastSeenAtUtc <= latestEventAtUtc)
        {
            return latestEventSource;
        }

        return mobilePresence.Source;
    }

    private static DateTime GetEffectiveVisitorLastSeenUtc(DateTime latestEventAtUtc, DateTime? qrWebPresenceLastSeenUtc, DateTime? mobilePresenceLastSeenUtc)
    {
        return new[] { latestEventAtUtc, qrWebPresenceLastSeenUtc, mobilePresenceLastSeenUtc }
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Max();
    }

    private static bool IsVisitorOnline(string? source, string deviceId, DateTime effectiveLastSeenAtUtc, DateTime onlineThresholdUtc, DateTime nowUtc)
    {
        if (IsQrWebVisitor(source, deviceId))
        {
            return effectiveLastSeenAtUtc >= nowUtc.Subtract(QrWebOnlineWindow);
        }

        if (IsMobilePresenceSource(source))
        {
            return effectiveLastSeenAtUtc >= nowUtc.Subtract(MobilePresenceOnlineWindow);
        }

        return effectiveLastSeenAtUtc >= onlineThresholdUtc;
    }

    private static bool IsMobilePresenceSource(string? source)
    {
        return (source ?? string.Empty).Contains("mobile-presence", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferDevicePlatform(string deviceId, string? source)
    {
        var combined = $"{deviceId} {source}".ToLowerInvariant();

        if (combined.Contains("iphone", StringComparison.Ordinal) || combined.Contains("ios", StringComparison.Ordinal) || combined.Contains("ipad", StringComparison.Ordinal))
        {
            return "iPhone";
        }

        if (combined.Contains("android", StringComparison.Ordinal) || combined.Contains("pixel", StringComparison.Ordinal) || combined.Contains("samsung", StringComparison.Ordinal) || combined.Contains("mi", StringComparison.Ordinal))
        {
            return "Android";
        }

        if (combined.Contains("windows", StringComparison.Ordinal))
        {
            return "Windows";
        }

        if (combined.Contains("mac", StringComparison.Ordinal))
        {
            return "Mac";
        }

        return "Visitor";
    }

    private static string NormalizeLanguageTag(string? languageCode)
    {
        return (languageCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi-VN",
            "en" or "en-us" or "en-gb" => "en-US",
            "ja" or "ja-jp" => "ja-JP",
            "ko" or "ko-kr" => "ko-KR",
            "zh" or "zh-cn" => "zh-CN",
            _ => string.Empty
        };
    }

    private static string InferLanguageTag(string? source, string deviceId)
    {
        var combined = $"{source} {deviceId}".ToLowerInvariant();

        if (combined.Contains("ja", StringComparison.Ordinal))
        {
            return "ja-JP";
        }

        if (combined.Contains("ko", StringComparison.Ordinal))
        {
            return "ko-KR";
        }

        if (combined.Contains("zh", StringComparison.Ordinal))
        {
            return "zh-CN";
        }

        if (combined.Contains("en", StringComparison.Ordinal))
        {
            return "en-US";
        }

        return "vi-VN";
    }

    private static bool LooksLikeOpaqueToken(string token)
    {
        if (token.Length < 8)
        {
            return false;
        }

        return token.All(Uri.IsHexDigit);
    }

    private static string MapDeviceToken(string token)
    {
        return token.ToLowerInvariant() switch
        {
            "android" => "Android",
            "ios" => "iPhone",
            "iphone" => "iPhone",
            "ipad" => "iPad",
            "windows" => "Windows",
            "macos" => "macOS",
            "mac" => "Mac",
            "browser" => "Browser",
            "web" => "Web",
            "guest" => "Guest",
            "tourist" => "Tourist",
            _ => char.ToUpperInvariant(token[0]) + token[1..]
        };
    }

    private static string GetDeviceSuffix(string deviceId)
    {
        var suffix = new string(deviceId.Where(char.IsLetterOrDigit).TakeLast(4).ToArray()).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(suffix) ? string.Empty : $"· {suffix}";
    }

    private sealed record VisitEventActivity(
        Guid? UserId,
        string DeviceId,
        int PoiId,
        EventType EventType,
        string? Source,
        DateTime CreatedAt);
}
