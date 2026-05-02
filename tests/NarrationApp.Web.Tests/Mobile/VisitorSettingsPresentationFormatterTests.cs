using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorSettingsPresentationFormatterTests
{
    [Fact]
    public void Visitor_mode_summary_matches_anonymous_first_mobile_copy()
    {
        var summary = VisitorSettingsPresentationFormatter.CreateVisitorModeSummary();

        Assert.Equal("Khách tham quan", summary.Title);
        Assert.Equal("Chế độ anonymous-first trên thiết bị hiện tại.", summary.Subtitle);
        Assert.Equal("Không cần tài khoản • chưa đồng bộ hồ sơ backend", summary.ModeLabel);
        Assert.Equal("GT", summary.Initials);
    }

    [Fact]
    public void Audio_and_gps_summaries_use_expected_labels()
    {
        var audioSummary = VisitorSettingsPresentationFormatter.FormatAudioSettingsSummary(
            autoPlayEnabled: true,
            sourcePreference: VisitorAudioSourcePreference.TextToSpeech,
            defaultPlaybackSpeed: 1.25d);
        var gpsSummary = VisitorSettingsPresentationFormatter.FormatGpsSettingsSummary(
            locationPermissionGranted: false,
            accuracyMode: VisitorGpsAccuracyMode.High);

        Assert.Equal("Tự phát bật • Google TTS • 1.25x", audioSummary);
        Assert.Equal("Vị trí đang tắt • High", gpsSummary);
    }

    [Fact]
    public void Settings_stats_and_cache_summary_are_formatted_for_cards()
    {
        var stats = VisitorSettingsPresentationFormatter.CreateSettingsStats(
            listenedPoiCount: 7,
            availableTourCount: 4,
            cachedAudioFileCount: 12);
        var cacheSummary = VisitorSettingsPresentationFormatter.FormatAudioCacheSummary(
            fileCount: 12,
            estimatedSizeMb: 48.35d);

        Assert.Collection(
            stats,
            item =>
            {
                Assert.Equal("7", item.Value);
                Assert.Equal("POI đã nghe", item.Label);
            },
            item =>
            {
                Assert.Equal("4", item.Value);
                Assert.Equal("Tour có sẵn", item.Label);
            },
            item =>
            {
                Assert.Equal("12", item.Value);
                Assert.Equal("File cache", item.Label);
            });
        Assert.Equal("12 file audio • 48.4 MB cache nội bộ", cacheSummary);
    }

    [Fact]
    public void Overview_summary_groups_settings_labels_for_the_overview_screen()
    {
        var summary = VisitorSettingsPresentationFormatter.CreateOverviewSummary(
            currentLanguageLabel: "Tiếng Việt",
            audioSummary: "Tự phát bật • Google TTS • 1.25x",
            gpsSummary: "Vị trí đang tắt • High",
            cacheSummary: "12 file audio • 48.4 MB cache nội bộ",
            historySummary: "5 lượt nghe • 2 lượt hoàn tất",
            aboutSummary: "v1.0.0 • .NET 9 Android Smoke-ready");

        Assert.Equal("Tiếng Việt", summary.CurrentLanguageLabel);
        Assert.Equal("Tự phát bật • Google TTS • 1.25x", summary.AudioSummary);
        Assert.Equal("Vị trí đang tắt • High", summary.GpsSummary);
        Assert.Equal("12 file audio • 48.4 MB cache nội bộ", summary.CacheSummary);
        Assert.Equal("5 lượt nghe • 2 lượt hoàn tất", summary.HistorySummary);
        Assert.Equal("v1.0.0 • .NET 9 Android Smoke-ready", summary.AboutSummary);
    }

    [Fact]
    public void Audio_pack_progress_style_handles_zero_total()
    {
        var progressStyle = VisitorSettingsPresentationFormatter.FormatAudioPackProgressStyle(totalPoiCount: 0, readyPoiCount: 0);

        Assert.Equal("width:0%;", progressStyle);
    }

    [Fact]
    public void Listening_history_messages_cover_empty_and_latest_entry_states()
    {
        var latestEntry = new VisitorListeningHistoryEntry(
            "entry-1",
            "poi-1",
            "Ốc Oanh",
            "Ẩm thực",
            "vi",
            "20:10",
            "2:30",
            85);

        var emptyHeadline = VisitorSettingsPresentationFormatter.FormatListeningHistoryHeadline(entryCount: 0, completedCount: 0);
        var emptyDescription = VisitorSettingsPresentationFormatter.FormatListeningHistoryDescription(null);
        var populatedHeadline = VisitorSettingsPresentationFormatter.FormatListeningHistoryHeadline(entryCount: 5, completedCount: 2);
        var populatedDescription = VisitorSettingsPresentationFormatter.FormatListeningHistoryDescription(latestEntry);

        Assert.Equal("Chưa có lịch sử nghe", emptyHeadline);
        Assert.Equal("Bắt đầu một tour hoặc mở POI để app ghi nhớ tiến trình nghe gần nhất.", emptyDescription);
        Assert.Equal("5 lượt nghe • 2 lượt hoàn tất", populatedHeadline);
        Assert.Equal("Mục gần nhất là Ốc Oanh • VI • 85% hoàn tất.", populatedDescription);
    }
}
