using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Server.Controllers;

public sealed partial class AdminController
{
    private static Guid CreateStableGuestId(string deviceId)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(deviceId.Trim().ToLowerInvariant()));
        return new Guid(hash);
    }

    private static AdminPoiDto BuildAdminPoiDto(Poi item, int? pendingModerationId = null) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            OwnerName = item.Owner?.FullName ?? "Unknown owner",
            OwnerEmail = item.Owner?.Email ?? "unknown@narration.app",
            Lat = item.Lat,
            Lng = item.Lng,
            Priority = item.Priority,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            Description = item.Description,
            TtsScript = item.TtsScript,
            Status = item.Status,
            AudioAssetCount = item.AudioAssets.Count,
            TranslationCount = item.Translations.Count,
            GeofenceCount = item.Geofences.Count,
            PendingModerationId = pendingModerationId,
            CreatedAtUtc = item.CreatedAt
        };

    private static string FormatGuestDeviceName(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return "Guest device";
        }

        if (string.Equals(deviceId, "anonymous-device", StringComparison.OrdinalIgnoreCase))
        {
            return "Anonymous device";
        }

        var tokens = deviceId
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !LooksLikeOpaqueToken(token))
            .ToArray();

        if (tokens.Length == 0)
        {
            return $"Guest device {GetDeviceSuffix(deviceId)}".Trim();
        }

        var words = tokens
            .Where(token => !string.Equals(token, "device", StringComparison.OrdinalIgnoreCase))
            .Select(MapDeviceToken)
            .Take(3)
            .ToArray();

        var label = words.Length == 0 ? "Guest device" : string.Join(" ", words);
        return $"{label} {GetDeviceSuffix(deviceId)}".Trim();
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

    private static string BuildEventLogCsv(IReadOnlyList<EventLogExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("event_id,created_at_utc,event_type,source,device_id,user_id,user_email,poi_id,poi_name,lat,lng,listen_duration_seconds");

        foreach (var row in rows)
        {
            AppendCsvRow(
                builder,
                row.Id.ToString(CultureInfo.InvariantCulture),
                row.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                row.EventType,
                row.Source,
                row.DeviceId,
                row.UserId?.ToString() ?? string.Empty,
                row.UserEmail,
                row.PoiId.ToString(CultureInfo.InvariantCulture),
                row.PoiName,
                FormatCsvDouble(row.Lat),
                FormatCsvDouble(row.Lng),
                row.ListenDurationSeconds.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string FormatCsvDouble(double? value) =>
        value.HasValue ? value.Value.ToString("0.######", CultureInfo.InvariantCulture) : string.Empty;

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendCsvValue(builder, values[index]);
        }

        builder.AppendLine();
    }

    private static void AppendCsvValue(StringBuilder builder, string value)
    {
        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
        builder.Append('"');
    }

    private sealed class EventLogExportRow
    {
        public long Id { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string EventType { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string DeviceId { get; init; } = string.Empty;

        public Guid? UserId { get; init; }

        public string UserEmail { get; init; } = string.Empty;

        public int PoiId { get; init; }

        public string PoiName { get; init; } = string.Empty;

        public double? Lat { get; init; }

        public double? Lng { get; init; }

        public int ListenDurationSeconds { get; init; }
    }
}
