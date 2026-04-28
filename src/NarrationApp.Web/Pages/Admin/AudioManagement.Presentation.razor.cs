using System.Globalization;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private IEnumerable<AudioDto> AllAudioItems => _audioByPoi.Values.SelectMany(items => items);
    private int TotalAudioCount => AllAudioItems.Count();
    private int ReadyAudioCount => AllAudioItems.Count(item => item.Status == AudioStatus.Ready);
    private int ProcessingAudioCount => AllAudioItems.Count(item => item.Status is AudioStatus.Requested or AudioStatus.Generating);
    private int FailedAudioCount => AllAudioItems.Count(item => item.Status == AudioStatus.Failed);
    private int DistinctLanguageCount => AllAudioItems.Select(item => item.LanguageCode).Where(code => !string.IsNullOrWhiteSpace(code)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    private bool HasProcessingAudio => AllAudioItems.Any(item => item.Status is AudioStatus.Requested or AudioStatus.Generating);
    private int PoisWithVietnameseSourceCount => _pois.Count(poi => GetVietnameseSource(GetAudioItems(poi.Id)) is not null);

    private string GetRowClass(AdminPoiDto poi) => poi.Id == _selectedPoi?.Id ? "is-active" : string.Empty;
    private bool IsPreviewingAudio(AudioDto audio) => _previewingAudioId == audio.Id;
    private bool IsPreviewLanguageSelected(int poiId, IEnumerable<AudioDto> items, string languageCode)
    {
        var selected = GetSelectedPreviewLanguageCode(poiId, items);
        return string.Equals(selected, languageCode, StringComparison.OrdinalIgnoreCase);
    }
    private bool CanPreviewLanguage(IEnumerable<AudioDto> items, string languageCode) =>
        GetCurrentAudioItems(items)
            .Any(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)
                && item.Status == AudioStatus.Ready);
    private static AudioDto? GetVietnameseSource(IEnumerable<AudioDto> items) =>
        items.Where(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .FirstOrDefault();

    private AudioDto? GetPreviewAudio(int poiId, IEnumerable<AudioDto> items, string? preferredLanguageCode = null)
    {
        var readyItems = GetCurrentAudioItems(items)
            .Where(item => item.Status == AudioStatus.Ready)
            .ToArray();

        if (readyItems.Length == 0)
        {
            return null;
        }

        var languageCode = preferredLanguageCode ?? GetSelectedPreviewLanguageCode(poiId, items);
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            var selected = readyItems.FirstOrDefault(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
            if (selected is not null)
            {
                return selected;
            }
        }

        return readyItems
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .First();
    }

    private string? GetSelectedPreviewLanguageCode(int poiId, IEnumerable<AudioDto> items)
    {
        if (_previewLanguageByPoi.TryGetValue(poiId, out var selected))
        {
            return selected;
        }

        var preview = GetCurrentAudioItems(items)
            .Where(item => item.Status == AudioStatus.Ready)
            .OrderByDescending(item => string.Equals(item.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .FirstOrDefault();

        return preview?.LanguageCode;
    }

    private static AudioDto? GetLatestAudio(IEnumerable<AudioDto> items) =>
        items.OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue).FirstOrDefault();

    private static AudioStatus? GetOverallAudioStatus(IEnumerable<AudioDto> items)
    {
        var audioItems = GetCurrentAudioItems(items).ToArray();
        if (audioItems.Any(item => item.Status == AudioStatus.Failed)) return AudioStatus.Failed;
        if (audioItems.Any(item => item.Status is AudioStatus.Requested or AudioStatus.Generating)) return AudioStatus.Generating;
        return audioItems.Any(item => item.Status == AudioStatus.Ready) ? AudioStatus.Ready : null;
    }

    private static IReadOnlyList<string> GetFailedLanguageCodes(IEnumerable<AudioDto> items) =>
        GetCurrentAudioItems(items)
            .Where(item => item.Status == AudioStatus.Failed)
            .Select(item => item.LanguageCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string GetSourceTagLabel(AudioDto? sourceAudio) => sourceAudio?.SourceType switch
    {
        AudioSourceType.Recorded => "Upload",
        AudioSourceType.Tts => "TTS",
        _ => "Chưa có"
    };

    private static string GetSourceTagClass(AudioDto? sourceAudio) => sourceAudio?.SourceType switch
    {
        AudioSourceType.Recorded => "audio-tag--recorded",
        AudioSourceType.Tts => "audio-tag--tts",
        _ => "audio-tag--muted"
    };

    private static string GetStatusLabel(AudioStatus? status) => status switch
    {
        AudioStatus.Ready => "ready",
        AudioStatus.Requested or AudioStatus.Generating => "generating",
        AudioStatus.Failed => "failed",
        _ => "empty"
    };

    private static string GetStatusTagClass(AudioStatus? status) => status switch
    {
        AudioStatus.Ready => "audio-tag--ready",
        AudioStatus.Requested or AudioStatus.Generating => "audio-tag--generating",
        AudioStatus.Failed => "audio-tag--failed",
        _ => "audio-tag--muted"
    };

    private string GetLanguageChipClass(int poiId, IEnumerable<AudioDto> items, string languageCode)
    {
        var statusClass = GetCurrentAudioItems(items)
            .Where(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault()?.Status switch
            {
                AudioStatus.Ready => "is-ready",
                AudioStatus.Requested or AudioStatus.Generating => "is-generating",
                AudioStatus.Failed => "is-failed",
                _ => "is-empty"
            };

        if (string.Equals(statusClass, "is-ready", StringComparison.Ordinal) && IsPreviewLanguageSelected(poiId, items, languageCode))
        {
            return $"{statusClass} is-selected";
        }

        return statusClass;
    }

    private static IEnumerable<AudioDto> GetCurrentAudioItems(IEnumerable<AudioDto> items) =>
        items.Where(item => item.Status is not AudioStatus.Deleted and not AudioStatus.Replaced)
            .GroupBy(item => item.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue).ThenByDescending(item => item.Id).First());

    private static string GetRelativeTimeLabel(DateTime? timestamp)
    {
        if (timestamp is null) return "Chưa cập nhật";
        var diff = DateTime.UtcNow - timestamp.Value.ToUniversalTime();
        if (diff.TotalMinutes < 60) return $"{Math.Max(1, (int)Math.Round(diff.TotalMinutes))} phút trước";
        if (diff.TotalHours < 24) return $"{Math.Max(1, (int)Math.Round(diff.TotalHours))} giờ trước";
        return $"{Math.Max(1, (int)Math.Round(diff.TotalDays))} ngày trước";
    }

    private static string FormatDuration(int durationSeconds) =>
        TimeSpan.FromSeconds(Math.Max(0, durationSeconds))
            .ToString(TimeSpan.FromSeconds(Math.Max(0, durationSeconds)).TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture);

    private double GetCoverageRatio()
    {
        if (_pois.Count == 0) return 0;
        var maxAssets = _pois.Count * Math.Max(ActiveLanguageOptions.Count, 1);
        return maxAssets == 0 ? 0 : (double)TotalAudioCount / maxAssets;
    }

    private double GetAssetRatio(int count) => TotalAudioCount == 0 ? 0 : (double)count / TotalAudioCount;

    private static string FormatPercent(double ratio, double floor = 0d)
    {
        var normalized = Math.Clamp(Math.Max(ratio, floor), 0d, 1d);
        return $"{normalized.ToString("P0", CultureInfo.InvariantCulture).Replace("%", string.Empty, StringComparison.Ordinal)}%";
    }
}
