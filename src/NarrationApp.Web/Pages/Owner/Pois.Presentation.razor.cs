using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class Pois
{
    private static string GetPoiStatusLabel(PoiStatus status) => status switch
    {
        PoiStatus.Published => "Đang xuất bản",
        PoiStatus.PendingReview => "Chờ duyệt",
        PoiStatus.Draft => "Nháp",
        PoiStatus.Rejected => "Rejected",
        PoiStatus.Updated => "Đã cập nhật",
        PoiStatus.Archived => "Lưu trữ",
        _ => status.ToString()
    };

    private static string GetCoordinateLabel(OwnerPoisWorkspaceRowDto row) => $"{row.Latitude:0.####}, {row.Longitude:0.####}";

    private static string GetSourceContentLabel(OwnerSourceContentKind kind) => kind switch
    {
        OwnerSourceContentKind.ScriptTts => "Script TTS",
        OwnerSourceContentKind.AudioFile => "Audio file",
        _ => "Chưa có"
    };

    private static string GetStatusText(PoiStatus status) => status switch
    {
        PoiStatus.Published => "Đang xuất bản",
        PoiStatus.PendingReview => "Chờ duyệt",
        PoiStatus.Rejected => "Rejected",
        PoiStatus.Updated => "Đã cập nhật",
        PoiStatus.Draft => "Nháp",
        _ => status.ToString()
    };

    private static string GetStatusClass(PoiStatus status) => status switch
    {
        PoiStatus.Published => "owner-workspace-badge owner-workspace-badge--good",
        PoiStatus.PendingReview => "owner-workspace-badge owner-workspace-badge--warn",
        PoiStatus.Rejected => "owner-workspace-badge owner-workspace-badge--danger",
        PoiStatus.Updated => "owner-workspace-badge owner-workspace-badge--source",
        _ => "owner-workspace-badge"
    };
}
