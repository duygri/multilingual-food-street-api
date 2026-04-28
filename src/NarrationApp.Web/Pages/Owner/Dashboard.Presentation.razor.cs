using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Pages.Owner;

public partial class Dashboard
{
    private static string GetRelativeTimeLabel(DateTime occurredAtUtc)
    {
        var elapsed = DateTime.UtcNow - occurredAtUtc;
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút trước";
        if (elapsed.TotalHours < 24) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày trước";
    }

    private static string GetActivityClass(OwnerDashboardRecentActivityDto item) => item.Tone switch
    {
        "good" => "owner-workspace-activity owner-workspace-activity--good",
        "info" => "owner-workspace-activity owner-workspace-activity--info",
        _ => "owner-workspace-activity"
    };
}
