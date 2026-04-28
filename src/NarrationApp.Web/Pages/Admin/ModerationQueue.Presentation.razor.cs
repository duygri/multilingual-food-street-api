using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Pages.Admin;

public partial class ModerationQueue
{
    private static string GetEntityTitle(ModerationRequestDto item) => item.EntityType switch
    {
        "poi" => $"POI #{item.EntityId} — Tạo mới nội dung",
        "owner_registration" => $"Owner #{item.EntityId} — Yêu cầu cấp quyền",
        _ => $"{item.EntityType} #{item.EntityId}"
    };

    private static string GetEntityMeta(ModerationRequestDto item) => $"{GetAgeLabel(item.CreatedAtUtc)} • {item.EntityType} • {item.RequestedBy.ToString("N")[..8]}";

    private static string GetAgeLabel(DateTime createdAtUtc)
    {
        var elapsed = DateTime.UtcNow - createdAtUtc;
        if (elapsed.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes))} phút trước";
        if (elapsed.TotalHours < 24) return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Floor(elapsed.TotalDays))} ngày trước";
    }
}
