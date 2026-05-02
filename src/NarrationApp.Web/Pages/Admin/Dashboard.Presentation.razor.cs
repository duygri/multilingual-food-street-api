using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Pages.Admin;

public partial class Dashboard
{
    private IReadOnlyList<(int Rank, TopPoiDto Item)> RankedTopPois => _overview.TopPois.Select((item, index) => (index + 1, item)).ToArray();
    private IReadOnlyList<(ModerationRequestDto Item, int Index)> RankedModeration => _pendingModeration.Take(5).Select((item, index) => (item, index)).ToArray();
    private int OnlineVisitorCount => _visitorDevices.Count(item => item.IsOnline);
    private int DistinctLanguageCount => _managedLanguages.Count(item => item.IsActive && !string.IsNullOrWhiteSpace(item.Code));
    private int PublishedToursCount => _overview.TotalTours;
    private string CurrentMonthTriggerHint => $"Geofence enter từ {GetCurrentMonthStartUtc():dd/MM}";

    private string GetLanguageHint()
    {
        var languages = _managedLanguages
            .Where(item => item.IsActive)
            .Select(item => item.Code)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .ToArray();

        return languages.Length == 0 ? "Chưa cấu hình" : string.Join(", ", languages);
    }

    private string GetModerationOwnerLabel(ModerationRequestDto item, int index)
    {
        var matchedUser = _users.FirstOrDefault(user => user.Id == item.RequestedBy);
        if (matchedUser is not null) return matchedUser.Email;
        if (_users.Count > 0) return _users[index % _users.Count].Email;
        return $"owner-{item.RequestedBy.ToString("N")[..6]}";
    }

    private static string GetModerationEntityTitle(ModerationRequestDto item) => item.EntityType switch { "poi" => $"POI #{item.EntityId}", "owner_registration" => $"Owner #{item.EntityId}", _ => $"{item.EntityType} #{item.EntityId}" };
    private static string GetModerationKind(ModerationRequestDto item) => item.EntityType switch { "poi" => "Tạo mới", "owner_registration" => "Đăng ký", _ => "Cập nhật" };

    private static string GetElapsedLabel(DateTime createdAtUtc)
    {
        var elapsed = DateTime.UtcNow - createdAtUtc;
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút trước";
        if (elapsed.TotalHours < 24) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày trước";
    }

    private static DateTime GetCurrentMonthStartUtc()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
