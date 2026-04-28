using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private static string GetPoiStatusLabel(PoiStatus status) => status switch
    {
        PoiStatus.Published => "Đã xuất bản",
        PoiStatus.PendingReview => "Chờ duyệt",
        PoiStatus.Draft => "Nháp",
        PoiStatus.Rejected => "Bị từ chối",
        PoiStatus.Updated => "Đã cập nhật",
        PoiStatus.Archived => "Lưu trữ",
        _ => status.ToString()
    };

    private static string GetPoiStatusClass(PoiStatus status) => status switch
    {
        PoiStatus.Published => "owner-workspace-badge owner-workspace-badge--good",
        PoiStatus.PendingReview => "owner-workspace-badge owner-workspace-badge--warn",
        PoiStatus.Rejected => "owner-workspace-badge owner-workspace-badge--danger",
        PoiStatus.Updated => "owner-workspace-badge owner-workspace-badge--source",
        _ => "owner-workspace-badge"
    };

    private static string GetNarrationLabel(NarrationMode mode) => mode switch
    {
        NarrationMode.TtsOnly => "Chỉ TTS",
        NarrationMode.RecordedOnly => "Chỉ audio nguồn",
        NarrationMode.Both => "Kết hợp",
        _ => mode.ToString()
    };

    private static string GetRadiusSummary(PoiDto poi)
    {
        var geofence = poi.Geofences.FirstOrDefault();
        return geofence is null ? "Chưa có radius" : $"{geofence.RadiusMeters}m";
    }

    private static string GetDurationLabel(int durationSeconds)
    {
        return durationSeconds <= 0 ? "Chưa có" : $"{durationSeconds}s";
    }

    private static string GetListenDurationLabel(double durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return "0:00";
        }

        var duration = TimeSpan.FromSeconds(durationSeconds);
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes}:{duration.Seconds:00}";
    }

    private static string GetAudioSourceLabel(AudioSourceType sourceType) => sourceType switch
    {
        AudioSourceType.Tts => "TTS",
        AudioSourceType.Recorded => "Audio nguồn",
        _ => sourceType.ToString()
    };

    private static string GetAudioStatusClass(AudioStatus status) => status switch
    {
        AudioStatus.Ready => "owner-workspace-badge owner-workspace-badge--good",
        AudioStatus.Generating => "owner-workspace-badge owner-workspace-badge--warn",
        AudioStatus.Failed => "owner-workspace-badge owner-workspace-badge--danger",
        _ => "owner-workspace-badge"
    };

    private static string GetModerationStatusLabel(ModerationStatus status) => status switch
    {
        ModerationStatus.Pending => "Chờ duyệt",
        ModerationStatus.Approved => "Đã duyệt",
        ModerationStatus.Rejected => "Bị từ chối",
        ModerationStatus.Revised => "Đã chỉnh sửa",
        _ => status.ToString()
    };

    private static string GetFileMetaLabel(IBrowserFile file)
    {
        return $"{file.ContentType} | {Math.Max(file.Size / 1024d, 1):0.#} KB";
    }
}
