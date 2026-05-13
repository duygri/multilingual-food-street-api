using System.Security.Cryptography;
using System.Text;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Services;

public sealed partial class VisitorDeviceDashboardService
{
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
