using System.Globalization;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorSettingsPresentationFormatter
{
    public static string GetAccountModeDescription() =>
        "Ứng dụng lưu tên hiển thị và email liên hệ tùy chọn ngay trên thiết bị, không yêu cầu tài khoản.";

    public static string FormatPlaybackSpeed(double speed) =>
        $"{speed.ToString("0.##", CultureInfo.InvariantCulture)}x";

    public static string FormatAudioSettingsSummary(
        bool autoPlayEnabled,
        VisitorAudioSourcePreference sourcePreference,
        double defaultPlaybackSpeed) =>
        $"{(autoPlayEnabled ? "Tự phát bật" : "Tự phát tắt")} • {GetAudioSourcePreferenceLabel(sourcePreference)} • {FormatPlaybackSpeed(defaultPlaybackSpeed)}";

    public static string FormatGpsSettingsSummary(
        bool locationPermissionGranted,
        VisitorGpsAccuracyMode accuracyMode) =>
        $"{(locationPermissionGranted ? "Vị trí đã bật" : "Vị trí đang tắt")} • {GetGpsAccuracyLabel(accuracyMode)}";

    public static IReadOnlyList<VisitorSettingsStat> CreateSettingsStats(
        int listenedPoiCount,
        int availableTourCount,
        int cachedAudioFileCount) =>
    [
        new VisitorSettingsStat(listenedPoiCount.ToString(CultureInfo.InvariantCulture), "POI đã nghe"),
        new VisitorSettingsStat(availableTourCount.ToString(CultureInfo.InvariantCulture), "Tour có sẵn"),
        new VisitorSettingsStat(cachedAudioFileCount.ToString(CultureInfo.InvariantCulture), "File cache")
    ];

    public static string FormatAudioPackSummary(int readyPoiCount) =>
        $"{readyPoiCount} gói sẵn sàng";

    public static string FormatAudioCacheSummary(int fileCount, double estimatedSizeMb) =>
        $"{fileCount} file audio • {estimatedSizeMb.ToString("0.0", CultureInfo.InvariantCulture)} MB cache nội bộ";

    public static string FormatAudioPackProgressStyle(int totalPoiCount, int readyPoiCount)
    {
        var total = Math.Max(1, totalPoiCount);
        var percent = readyPoiCount * 100d / total;
        return $"width:{percent.ToString("0.##", CultureInfo.InvariantCulture)}%;";
    }

    public static string FormatListeningHistoryHeadline(int entryCount, int completedCount) =>
        entryCount == 0
            ? "Chưa có lịch sử nghe"
            : $"{entryCount} lượt nghe • {completedCount} lượt hoàn tất";

    public static string FormatListeningHistoryDescription(VisitorListeningHistoryEntry? latestEntry)
    {
        if (latestEntry is null)
        {
            return "Bắt đầu một tour hoặc mở POI để app ghi nhớ tiến trình nghe gần nhất.";
        }

        return $"Mục gần nhất là {latestEntry.PoiName} • {latestEntry.LanguageCode.ToUpperInvariant()} • {latestEntry.CompletionPercent}% hoàn tất.";
    }

    private static string GetAudioSourcePreferenceLabel(VisitorAudioSourcePreference preference) =>
        preference switch
        {
            VisitorAudioSourcePreference.TextToSpeech => "Google TTS",
            _ => "Recorded trước"
        };

    private static string GetGpsAccuracyLabel(VisitorGpsAccuracyMode mode) =>
        mode switch
        {
            VisitorGpsAccuracyMode.High => "High",
            VisitorGpsAccuracyMode.BatterySaver => "Battery",
            _ => "Adaptive"
        };
}
