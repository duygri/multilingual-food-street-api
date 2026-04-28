using NarrationApp.Shared.DTOs.Notification;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Pages.Owner;

public partial class Notifications
{
    private bool MatchesReadFilter(NotificationDto item) => _readFilter switch
    {
        "unread" => !item.IsRead,
        "read" => item.IsRead,
        _ => true
    };

    private bool MatchesTypeFilter(NotificationDto item) =>
        string.Equals(_typeFilter, "all", StringComparison.Ordinal)
        || string.Equals(item.Type.ToString(), _typeFilter, StringComparison.Ordinal);

    private static string GetNotificationItemClass(NotificationDto item) =>
        item.IsRead ? "panel-shell owner-notifications-item is-read" : "panel-shell owner-notifications-item is-unread";

    private static string GetNotificationTypeLabel(NotificationType type) => type switch
    {
        NotificationType.ModerationResult => "Moderation",
        NotificationType.AudioReady => "Audio ready",
        NotificationType.TourPublished => "Tour published",
        NotificationType.PoiUpdated => "POI updated",
        NotificationType.System => "System",
        _ => type.ToString()
    };

    private static StatusTone GetNotificationTypeTone(NotificationType type) => type switch
    {
        NotificationType.ModerationResult => StatusTone.Warn,
        NotificationType.AudioReady => StatusTone.Good,
        NotificationType.TourPublished => StatusTone.Info,
        NotificationType.PoiUpdated => StatusTone.Info,
        NotificationType.System => StatusTone.Neutral,
        _ => StatusTone.Neutral
    };

    private static string GetRelativeTimeLabel(DateTime createdAtUtc)
    {
        var elapsed = DateTime.UtcNow - createdAtUtc;
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút trước";
        if (elapsed.TotalHours < 24) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày trước";
    }
}
