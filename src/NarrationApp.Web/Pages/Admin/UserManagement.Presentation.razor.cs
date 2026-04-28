using NarrationApp.Shared.DTOs.Admin;

namespace NarrationApp.Web.Pages.Admin;

public partial class UserManagement
{
    private static string GetIdentityLine(VisitorDeviceSummaryDto visitor) => visitor.DeviceId;

    private static string GetLanguageLabel(string? languageTag) => string.IsNullOrWhiteSpace(languageTag) ? "-" : languageTag.Trim();

    private static string GetLastSeenAbsolute(DateTime? lastSeenAtUtc)
    {
        if (lastSeenAtUtc is null) return "Chưa có tín hiệu";
        return lastSeenAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    private static string GetLastSeenRelative(DateTime? lastSeenAtUtc)
    {
        if (lastSeenAtUtc is null) return "Chưa ghi nhận";
        var elapsed = DateTime.UtcNow - lastSeenAtUtc.Value;
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút trước";
        if (elapsed.TotalHours < 24) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày trước";
    }
}
