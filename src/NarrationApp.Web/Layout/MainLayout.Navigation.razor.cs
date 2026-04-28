using System.Security.Claims;
using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Layout;

public partial class MainLayout
{
    private static IReadOnlyList<ShellNavItem> BuildNavigation(ClaimsPrincipal user, OwnerShellSummaryDto? ownerSummary)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        return role switch
        {
            "admin" =>
            [
                new ShellNavItem { Group = "Tổng quan", Label = "Dashboard", Description = "Kiểm duyệt và người dùng", Href = "/admin/dashboard", IconGlyph = "◈" },
                new ShellNavItem { Group = "Nội dung", Label = "Quản lý POI", Description = "Danh sách tất cả owner", Href = "/admin/poi-management", IconGlyph = "◎" },
                new ShellNavItem { Group = "Nội dung", Label = "Audio", Description = "Audio nguồn và đa ngôn ngữ", Href = "/admin/audio-management", IconGlyph = "▶" },
                new ShellNavItem { Group = "Nội dung", Label = "Bản dịch", Description = "Google Cloud Translation", Href = "/admin/translation-review", IconGlyph = "✦" },
                new ShellNavItem { Group = "Nội dung", Label = "Danh mục", Description = "Nhóm món ăn và đồ uống", Href = "/admin/category-management", IconGlyph = "▤" },
                new ShellNavItem { Group = "Nội dung", Label = "Tour", Description = "Tuyến và điểm dừng", Href = "/admin/tour-management", IconGlyph = "⬡" },
                new ShellNavItem { Group = "Nội dung", Label = "QR Codes", Description = "Mã quét và deep link", Href = "/admin/qr-management", IconGlyph = "□" },
                new ShellNavItem { Group = "Vận hành", Label = "Kiểm duyệt", Description = "Duyệt và từ chối", Href = "/admin/moderation-queue", IconGlyph = "▣" },
                new ShellNavItem { Group = "Hệ thống", Label = "Phân tích", Description = "Heatmap và audio", Href = "/admin/analytics", IconGlyph = "◷" },
                new ShellNavItem { Group = "Hệ thống", Label = "Ngôn ngữ", Description = "Bật và theo dõi coverage", Href = "/admin/language-management", IconGlyph = "◌" },
                new ShellNavItem { Group = "Vận hành", Label = "Visitor devices", Description = "Tổng visitor và thiết bị online", Href = "/admin/visitor-devices", IconGlyph = "⊙" }
            ],
            "poi_owner" =>
            [
                new ShellNavItem { Group = "Tổng quan", Label = "Dashboard", Description = "POI và audio", Href = "/owner/dashboard", IconGlyph = "◈", Match = ShellNavItemMatch.Exact },
                new ShellNavItem { Group = "Nội dung", Label = "POI", Description = "Danh sách và chi tiết", Href = "/owner/pois", IconGlyph = "◎", Match = ShellNavItemMatch.Prefix },
                new ShellNavItem { Group = "Nội dung", Label = "Tạo POI mới", Description = "Tạo bản nháp mới", Href = "/owner/pois/new", IconGlyph = "+", Match = ShellNavItemMatch.Exact },
                new ShellNavItem { Group = "Vận hành", Label = "Moderation", Description = "Theo dõi kiểm duyệt", Href = "/owner/moderation", IconGlyph = "▣", BadgeText = FormatBadge(ownerSummary?.PendingModerationRequests), Match = ShellNavItemMatch.Exact },
                new ShellNavItem { Group = "Vận hành", Label = "Notifications", Description = "Lịch sử thông báo", Href = "/owner/notifications", IconGlyph = "◌", BadgeText = FormatBadge(ownerSummary?.UnreadNotifications), Match = ShellNavItemMatch.Exact },
                new ShellNavItem { Group = "Tài khoản", Label = "Profile", Description = "Hồ sơ owner", Href = "/owner/profile", IconGlyph = "⊙", Match = ShellNavItemMatch.Exact }
            ],
            _ => Array.Empty<ShellNavItem>()
        };
    }

    private static string FormatCount(int? count) => count.HasValue ? count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "—";

    private static string? FormatBadge(int? count) =>
        count.GetValueOrDefault() > 0
            ? count.GetValueOrDefault().ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;

    private static string GetOwnerInitials(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "OW";
        }

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "OW";
        }

        if (parts.Length == 1)
        {
            var singleWordInitials = new string(parts[0].Where(char.IsLetterOrDigit).Take(2).Select(char.ToUpperInvariant).ToArray());
            return string.IsNullOrWhiteSpace(singleWordInitials) ? "OW" : singleWordInitials;
        }

        var combined = string.Concat(GetInitial(parts[0]), GetInitial(parts[^1]));
        return string.IsNullOrWhiteSpace(combined) ? "OW" : combined;
    }

    private static string GetInitial(string value)
    {
        var initial = value.FirstOrDefault(char.IsLetterOrDigit);
        return initial == default ? string.Empty : char.ToUpperInvariant(initial).ToString();
    }
}
