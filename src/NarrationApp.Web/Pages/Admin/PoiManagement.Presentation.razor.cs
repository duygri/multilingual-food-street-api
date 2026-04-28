using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private bool CanEdit(AdminPoiDto poi) => poi.Status is PoiStatus.Draft or PoiStatus.Published or PoiStatus.Updated;

    private static string GetFilterLabel(PoiFilterTab filter) => filter switch
    {
        PoiFilterTab.Published => "Đã xuất bản",
        PoiFilterTab.Pending => "Chờ duyệt",
        PoiFilterTab.Archived => "Lưu trữ",
        _ => "Tất cả"
    };

    private static string GetFilterActionName(PoiFilterTab filter) => filter switch
    {
        PoiFilterTab.Published => "published",
        PoiFilterTab.Pending => "pending",
        PoiFilterTab.Archived => "archived",
        _ => "all"
    };

    private static string GetStatusLabel(PoiStatus status) => status switch
    {
        PoiStatus.Draft => "Nháp",
        PoiStatus.PendingReview => "Chờ duyệt",
        PoiStatus.Published => "Đã xuất bản",
        PoiStatus.Rejected => "Từ chối",
        PoiStatus.Updated => "Đã cập nhật",
        PoiStatus.Archived => "Lưu trữ",
        _ => status.ToString()
    };

    private static StatusTone GetStatusTone(PoiStatus status) => status switch
    {
        PoiStatus.Published => StatusTone.Good,
        PoiStatus.PendingReview => StatusTone.Warn,
        PoiStatus.Rejected => StatusTone.Critical,
        PoiStatus.Updated => StatusTone.Info,
        _ => StatusTone.Neutral
    };

    private static string GetLanguageLabel(int translationCount) =>
        translationCount == 1 ? "1 ngôn ngữ" : $"{translationCount} ngôn ngữ";

    private static string GetAudioLabel(AdminPoiDto poi)
    {
        if (poi.AudioAssetCount > 0)
        {
            return "Có sẵn";
        }

        return poi.Status switch
        {
            PoiStatus.PendingReview or PoiStatus.Updated => "Đang tạo",
            PoiStatus.Archived or PoiStatus.Rejected => "Lỗi",
            _ => "Chưa có"
        };
    }

    private static StatusTone GetAudioTone(AdminPoiDto poi)
    {
        if (poi.AudioAssetCount > 0)
        {
            return StatusTone.Good;
        }

        return poi.Status switch
        {
            PoiStatus.PendingReview or PoiStatus.Updated => StatusTone.Warn,
            PoiStatus.Archived or PoiStatus.Rejected => StatusTone.Critical,
            _ => StatusTone.Neutral
        };
    }

    private static string FormatCoordinates(AdminPoiDto poi) =>
        FormattableString.Invariant($"{poi.Lat:0.0000}, {poi.Lng:0.0000}");
}
