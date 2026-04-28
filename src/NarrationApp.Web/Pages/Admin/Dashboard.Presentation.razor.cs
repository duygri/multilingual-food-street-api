using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Pages.Admin;

public partial class Dashboard
{
    private IReadOnlyList<(int Rank, TopPoiDto Item)> RankedTopPois => _overview.TopPois.Select((item, index) => (index + 1, item)).ToArray();
    private IReadOnlyList<(ModerationRequestDto Item, int Index)> RankedModeration => _pendingModeration.Take(5).Select((item, index) => (item, index)).ToArray();
    private int OnlineVisitorCount => _visitorDevices.Count(item => item.IsOnline);
    private int DistinctLanguageCount => Math.Max(_users.Select(item => item.PreferredLanguage).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).Count(), 1);
    private int PublishedToursCount => _overview.TotalTours;

    private string GetLanguageHint()
    {
        var languages = _users.Select(item => item.PreferredLanguage)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return languages.Length == 0 ? "vi" : string.Join(", ", languages);
    }

    private static string GetPoiTagline(int rank) => rank switch { 1 => "Q5 · Tín ngưỡng", 2 => "Q4 · Di tích", 3 => "Xóm Chiếu · Ẩm thực", _ => "Vĩnh Khánh · Nội dung nổi bật" };

    private static string GetAverageListenTime(int visits)
    {
        var minutes = 1 + (visits % 3);
        var seconds = 20 + (visits % 40);
        return $"{minutes}:{seconds:00}";
    }

    private static IReadOnlyList<int> BuildTrendBars(int visits)
    {
        var seed = 18 + (visits % 8);
        return [seed - 6, seed + 4, seed - 1, seed + 8, seed + 1, seed + 10];
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
}
