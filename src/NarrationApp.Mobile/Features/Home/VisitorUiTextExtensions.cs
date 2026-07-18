using System.Globalization;
using System.Text;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorUiTextExtensions
{
    public static string BrandTagline(this VisitorUiText text) =>
        text.Pick("Hear the streets, taste Saigon.", "Nghe chuyện phố, nếm vị Sài Gòn.");

    public static string BottomNavigationLabel(this VisitorUiText text) =>
        text.Pick("Primary navigation", "Điều hướng chính");

    public static bool UsesEnglishUi(this VisitorUiText text) =>
        string.Equals(text.TabMap, "Map", StringComparison.OrdinalIgnoreCase);

    public static string Pick(this VisitorUiText text, string english, string vietnamese) =>
        text.UsesEnglishUi() ? english : vietnamese;

    public static string SearchInputHint(this VisitorUiText text) =>
        text.Pick("Type POI name", "Gõ tên POI");

    public static string SearchSummaryDescription(this VisitorUiText text) =>
        text.Pick(
            "Tap a POI or tour to open the right narration context quickly.",
            "Chạm vào POI hoặc tour để mở nhanh đúng ngữ cảnh nghe thuyết minh.");

    public static string FormatSearchResultCount(this VisitorUiText text, int resultCount) =>
        text.UsesEnglishUi()
            ? resultCount switch
            {
                0 => "No matching results",
                1 => "Found 1 result",
                _ => $"Found {resultCount} results"
            }
            : VisitorSearchPresentationFormatter.FormatResultCountLabel(resultCount);

    public static string SearchPoiSectionTitle(this VisitorUiText text) =>
        text.Pick("Points of interest", "Điểm tham quan");

    public static string SearchTourSectionTitle(this VisitorUiText text) =>
        text.Pick("Related tours", "Tour liên quan");

    public static string SearchEmptyTourTitle(this VisitorUiText text) =>
        text.Pick("No matching tours.", "Chưa có tour phù hợp.");

    public static string SearchEmptyTourDescription(this VisitorUiText text) =>
        text.Pick("Try another tour name or related stop.", "Tìm thêm theo tên tour hoặc điểm dừng liên quan.");

    public static string NearbyPoiLabel(this VisitorUiText text) =>
        text.Pick("Nearby POI", "POI gần bạn");

    public static string SuggestedTourLabel(this VisitorUiText text) =>
        text.Pick("Suggested tour", "Tour gợi ý");

    public static string TourListTitle(this VisitorUiText text) =>
        text.Pick("Discovery tours", "Tour khám phá");

    public static string TourListSubtitle(this VisitorUiText text) =>
        text.Pick("Ready-made routes - just follow along", "Lộ trình được vạch sẵn - chỉ cần đi theo");

    public static string TourReadyRoutesLabel(this VisitorUiText text) =>
        text.Pick("Ready routes", "Lộ trình sẵn sàng");

    public static string TourTotalStopsLabel(this VisitorUiText text) =>
        text.Pick("Total stops", "Tổng điểm dừng");

    public static string TourAveragePaceLabel(this VisitorUiText text) =>
        text.Pick("Average pace", "Nhịp tour trung bình");

    public static string PopularBadgeLabel(this VisitorUiText text) =>
        text.Pick("Popular", "Phổ biến");

    public static string NewBadgeLabel(this VisitorUiText text) =>
        text.Pick("New", "Mới");

    public static string CultureBadgeLabel(this VisitorUiText text) =>
        text.Pick("Culture", "Văn hóa");

    public static string FormatParticipantLabel(this VisitorUiText text, int count) =>
        text.UsesEnglishUi() ? $"{count} joined" : $"{count} đã tham gia";

    public static string TourSelectedLabel(this VisitorUiText text) =>
        text.Pick("Selected tour", "Tour đã chọn");

    public static string FormatTourOverviewDescription(this VisitorUiText text, string actionLabel) =>
        text.UsesEnglishUi()
            ? $"{actionLabel.ToLowerInvariant()} so the app can guide you through each stop and keep your listening progress."
            : $"{actionLabel.ToLowerInvariant()} để app dẫn bạn qua từng điểm dừng và tiếp tục lưu tiến trình nghe.";

    public static string TourStopsLabel(this VisitorUiText text) =>
        text.Pick("Stops", "Điểm dừng");

    public static string TourDurationLabel(this VisitorUiText text) =>
        text.Pick("Duration", "Thời gian");

    public static string TourDistanceLabel(this VisitorUiText text) =>
        text.Pick("Distance", "Quãng đường");

    public static string TourParticipantsLabel(this VisitorUiText text) =>
        text.Pick("Joined", "Đã tham gia");

    public static string TourProgressTitle(this VisitorUiText text) =>
        text.Pick("Your progress", "Tiến trình của bạn");

    public static string TourProgressDescription(this VisitorUiText text) =>
        text.Pick(
            "Follow the suggested route to unlock each stop at the right tour rhythm.",
            "Đi theo lộ trình đã gợi ý để mở từng điểm dừng theo đúng nhịp tour.");

    public static string TourRouteLabel(this VisitorUiText text) =>
        text.Pick("Route", "Lộ trình");

    public static string BackButtonLabel(this VisitorUiText text) =>
        text.Pick("Back", "Quay lại");

    public static string AudioSettingsDescription(this VisitorUiText text) =>
        text.Pick(
            "Tune autoplay, preferred audio source, and default playback speed.",
            "Tinh chỉnh cách mobile tự phát, ưu tiên nguồn audio và tốc độ nghe mặc định.");

    public static string AutoPlaySectionLabel(this VisitorUiText text) =>
        text.Pick("Autoplay", "Tự động phát");

    public static string AutoPlayGeofenceTitle(this VisitorUiText text) =>
        text.Pick("Play when entering a geofence", "Phát khi vào geofence");

    public static string AutoPlayGeofenceDescription(this VisitorUiText text) =>
        text.Pick(
            "Start audio as soon as you enter the configured trigger radius.",
            "Bật audio ngay khi bạn bước vào đúng bán kính kích hoạt.");

    public static string SpokenAnnouncementsTitle(this VisitorUiText text) =>
        text.Pick("Voice announcements", "Thông báo bằng giọng nói");

    public static string SpokenAnnouncementsDescription(this VisitorUiText text) =>
        text.Pick(
            "Read a short prompt before the main narration starts.",
            "Đọc nhắc ngắn trước khi bắt đầu phần thuyết minh chính.");

    public static string AutoAdvanceTitle(this VisitorUiText text) =>
        text.Pick("Auto-advance to next POI", "Tự chuyển POI tiếp theo");

    public static string AutoAdvanceDescription(this VisitorUiText text) =>
        text.Pick(
            "When following a tour, suggest the next stop after narration finishes.",
            "Khi đang theo tour, app sẽ gợi ý chuyển sang điểm kế tiếp sau khi nghe xong.");

    public static string AudioSourcePreferenceLabel(this VisitorUiText text) =>
        text.Pick("Preferred audio source", "Nguồn audio ưu tiên");

    public static string DefaultPlaybackSpeedLabel(this VisitorUiText text) =>
        text.Pick("Default playback speed", "Tốc độ phát mặc định");

    public static string ToggleLabel(this VisitorUiText text, bool isEnabled) =>
        isEnabled ? text.Pick("On", "Bật") : text.Pick("Off", "Tắt");

    public static string SourcePreferenceLabel(this VisitorUiText text, VisitorAudioSourcePreference preference) =>
        preference == VisitorAudioSourcePreference.TextToSpeech
            ? "Google TTS"
            : text.Pick("Recorded first", "Recorded trước");

    public static string SourcePreferenceDescription(this VisitorUiText text, VisitorAudioSourcePreference preference) =>
        preference == VisitorAudioSourcePreference.TextToSpeech
            ? text.Pick(
                "Prefer Google Cloud TTS audio when it is ready in the selected language.",
                "Ưu tiên audio generate bằng Google Cloud TTS khi có sẵn đúng ngôn ngữ.")
            : text.Pick(
                "Prefer original recorded audio, then fall back to TTS when no matching file exists.",
                "Ưu tiên bản thu gốc tiếng Việt/recorded, chỉ rơi về TTS khi chưa có file phù hợp.");

    public static string GpsSettingsDescription(this VisitorUiText text) =>
        text.Pick(
            "Adjust permissions, accuracy, and how the map follows your current position.",
            "Điều chỉnh quyền truy cập, mức chính xác và cách map tự focus theo vị trí hiện tại.");

    public static string CurrentStatusLabel(this VisitorUiText text) =>
        text.Pick("Current status", "Trạng thái hiện tại");

    public static string LocationEnabledLabel(this VisitorUiText text) =>
        text.Pick("Location enabled", "Vị trí đã bật");

    public static string LocationDisabledLabel(this VisitorUiText text) =>
        text.Pick("Location disabled", "Vị trí đang tắt");

    public static string BatteryLabel(this VisitorUiText text, int batteryPercent) =>
        text.Pick($"Battery {batteryPercent}%", $"Pin {batteryPercent}%");

    public static string BackgroundTrackingLabel(this VisitorUiText text) =>
        text.Pick("Background tracking", "Theo dõi nền");

    public static string BackgroundGeofenceTitle(this VisitorUiText text) =>
        text.Pick("Allow background geofence", "Cho phép geofence khi app nền");

    public static string BackgroundGeofenceDescription(this VisitorUiText text) =>
        text.Pick(
            "Keep detecting nearby POIs while you walk or lock the screen.",
            "Giữ trạng thái nhận biết POI gần bạn khi đang đi bộ hoặc khóa màn hình.");

    public static string GpsAutoFocusTitle(this VisitorUiText text) =>
        text.Pick("Auto-focus current location", "Tự focus vị trí hiện tại");

    public static string GpsAutoFocusDescription(this VisitorUiText text) =>
        text.Pick(
            "Prioritize returning the map to your area when opening the app or switching tabs.",
            "Map sẽ ưu tiên quay lại khu vực bạn đang đứng khi mở app hoặc đổi tab.");

    public static string GpsAccuracyLabel(this VisitorUiText text) =>
        text.Pick("GPS accuracy", "Mức chính xác GPS");

    public static string GpsDebugLogLabel(this VisitorUiText text) =>
        text.Pick("Geofence log", "Nhật ký geofence");

    public static string GpsDebugEmptyTitle(this VisitorUiText text) =>
        text.Pick("No overlap events yet", "Chưa có sự kiện overlap");

    public static string GpsDebugEmptyDescription(this VisitorUiText text) =>
        text.Pick(
            "Walk into a POI radius to see how the app selects active, queued, and promoted stops.",
            "Đi bộ vào vùng POI để xem app chọn active, queue và promote như thế nào.");

    public static string AccuracyDescription(this VisitorUiText text, VisitorGpsAccuracyMode mode) =>
        mode switch
        {
            VisitorGpsAccuracyMode.High => text.Pick(
                "Prioritize maximum accuracy for geofence and autoplay, with higher battery use.",
                "Ưu tiên độ chính xác tối đa cho geofence và auto-play, đổi lại sẽ dùng pin nhiều hơn."),
            VisitorGpsAccuracyMode.BatterySaver => text.Pick(
                "Reduce location update frequency for lighter tours and better battery life.",
                "Giảm tần suất cập nhật vị trí khi bạn chỉ muốn theo tour nhẹ và ít hao pin."),
            _ => text.Pick(
                "Balance smooth walking detection with all-day battery usage.",
                "Cân bằng giữa độ mượt khi đi bộ và mức tiêu thụ pin trong ngày.")
        };

    public static string CacheDescription(this VisitorUiText text) =>
        text.Pick(
            "Keep POI, tour, and audio snapshots for weak network conditions. True offline basemaps are outside this local cache.",
            "Lưu snapshot POI, tour và audio theo ngôn ngữ hiện tại để app vẫn dùng được khi mạng yếu. Bản đồ nền offline thật chưa nằm trong phạm vi này.");

    public static string StoredDataLabel(this VisitorUiText text) =>
        text.Pick("Data stored on this device", "Dữ liệu đang giữ trên máy");

    public static string FormatStoredAudioCount(this VisitorUiText text, int audioCount, int poiCount) =>
        text.UsesEnglishUi()
            ? $"{audioCount} audio files across {poiCount} different POIs."
            : $"{audioCount} file audio thuộc {poiCount} POI khác nhau.";

    public static string CachedAudioStorageLabel(this VisitorUiText text) =>
        text.Pick("Cached audio storage", "Dung lượng audio đã cache");

    public static string CachedAudioStorageDescription(this VisitorUiText text) =>
        text.Pick(
            "Preload audio for the selected narration language so QR and geofence playback still work.",
            "Tải trước audio cho ngôn ngữ đang chọn để quét QR hoặc vào geofence vẫn nghe được.");

    public static string RescanCacheButtonLabel(this VisitorUiText text) =>
        text.Pick("Rescan cache", "Quét lại cache");

    public static string ClearAllButtonLabel(this VisitorUiText text) =>
        text.Pick("Clear all", "Xóa tất cả");

    public static string DeleteButtonLabel(this VisitorUiText text) =>
        text.Pick("Delete", "Xóa");

    public static string EmptyCacheTitle(this VisitorUiText text) =>
        text.Pick("No local audio files yet.", "Chưa có file audio cục bộ.");

    public static string EmptyCacheDescription(this VisitorUiText text) =>
        text.Pick(
            "Download or listen to a few POIs so the app can keep audio for later.",
            "Tải hoặc nghe một vài POI để app lưu cache cho lần sau.");

    public static string FormatPreloadAudioLabel(this VisitorUiText text, string languageLabel) =>
        text.UsesEnglishUi() ? $"Download {languageLabel} audio" : $"Tải audio {languageLabel}";

    public static string ReadyPreloadStatus(this VisitorUiText text) =>
        text.Pick("Ready to preload audio for the current language.", "Sẵn sàng tải trước audio cho ngôn ngữ hiện tại.");

    public static string FormatPreparingCacheStatus(this VisitorUiText text, string languageLabel) =>
        text.UsesEnglishUi()
            ? $"Preparing content/audio cache for {languageLabel}..."
            : $"Đang chuẩn bị cache nội dung/audio {languageLabel}...";

    public static string FormatPreloadFailure(this VisitorUiText text, string message) =>
        text.UsesEnglishUi() ? $"Could not preload audio: {message}" : $"Không tải trước được audio: {message}";

    public static string FormatPreloadResult(this VisitorUiText text, VisitorAudioPreloadResult result)
    {
        if (result.Total == 0)
        {
            return text.Pick(
                "No POI has ready audio for the current language.",
                "Không có POI nào có audio sẵn cho ngôn ngữ hiện tại.");
        }

        return result.Failed > 0
            ? text.Pick(
                $"Downloaded {result.Downloaded}, skipped {result.Skipped}, failed {result.Failed}.",
                $"Đã tải {result.Downloaded}, bỏ qua {result.Skipped}, lỗi {result.Failed}.")
            : text.Pick(
                $"Audio cache ready: {result.Downloaded} new, {result.Skipped} already cached.",
                $"Cache audio đã sẵn sàng: tải mới {result.Downloaded}, đã có {result.Skipped}.");
    }

    public static string ListenHistoryDescription(this VisitorUiText text) =>
        text.Pick(
            "Review POIs you recently opened and jump back to the stop you were listening to.",
            "Xem lại những POI gần đây bạn đã mở và chạm để quay lại đúng điểm đang nghe dở.");

    public static string EmptyHistoryTitle(this VisitorUiText text) =>
        text.Pick("No listening history yet.", "Chưa có lịch sử nghe.");

    public static string EmptyHistoryDescription(this VisitorUiText text) =>
        text.Pick(
            "When you start a tour or listen to a POI, the app will save the timeline here.",
            "Khi bạn bắt đầu một tour hoặc nghe POI, app sẽ lưu lại timeline ở đây.");

    public static string CompletionLabel(this VisitorUiText text, VisitorListeningHistoryEntry entry) =>
        entry.CompletionPercent >= 100
            ? text.Pick("Completed", "Nghe xong")
            : text.Pick($"Listened {entry.CompletionPercent}%", $"Đã nghe {entry.CompletionPercent}%");

    public static string AboutDescription(this VisitorUiText text) =>
        text.Pick(
            "Mobile build details, runtime stack, and support links for cross-checking the spec.",
            "Thông tin build mobile, stack đang dùng và các liên kết hỗ trợ khi cần đối chiếu spec.");

    public static VisitorModeSummary CreateVisitorModeSummary(this VisitorUiText text) =>
        text.UsesEnglishUi()
            ? new(
                Title: "Visitor",
                Subtitle: "Anonymous-first mode on this device.",
                ModeLabel: "No account required • backend profile not synced",
                Initials: "GT")
            : VisitorSettingsPresentationFormatter.CreateVisitorModeSummary();

    public static string FormatAudioSettingsSummary(
        this VisitorUiText text,
        bool autoPlayEnabled,
        VisitorAudioSourcePreference sourcePreference,
        double defaultPlaybackSpeed) =>
        text.UsesEnglishUi()
            ? $"{(autoPlayEnabled ? "Autoplay on" : "Autoplay off")} • {text.SourcePreferenceLabel(sourcePreference)} • {VisitorSettingsPresentationFormatter.FormatPlaybackSpeed(defaultPlaybackSpeed)}"
            : VisitorSettingsPresentationFormatter.FormatAudioSettingsSummary(autoPlayEnabled, sourcePreference, defaultPlaybackSpeed);

    public static string FormatGpsSettingsSummary(
        this VisitorUiText text,
        bool locationPermissionGranted,
        VisitorGpsAccuracyMode accuracyMode) =>
        text.UsesEnglishUi()
            ? $"{(locationPermissionGranted ? "Location enabled" : "Location disabled")} • {accuracyMode}"
            : VisitorSettingsPresentationFormatter.FormatGpsSettingsSummary(locationPermissionGranted, accuracyMode);

    public static IReadOnlyList<VisitorSettingsStat> CreateSettingsStats(
        this VisitorUiText text,
        int listenedPoiCount,
        int availableTourCount,
        int cachedAudioFileCount) =>
        text.UsesEnglishUi()
            ? new VisitorSettingsStat[]
            {
                new(listenedPoiCount.ToString(CultureInfo.InvariantCulture), "Listened POIs"),
                new(availableTourCount.ToString(CultureInfo.InvariantCulture), "Available tours"),
                new(cachedAudioFileCount.ToString(CultureInfo.InvariantCulture), "Cached files")
            }
            : VisitorSettingsPresentationFormatter.CreateSettingsStats(listenedPoiCount, availableTourCount, cachedAudioFileCount);

    public static string FormatOfflinePackSummary(
        this VisitorUiText text,
        int poiCount,
        int tourCount,
        int cachedAudioFileCount,
        double estimatedSizeMb) =>
        text.UsesEnglishUi()
            ? $"{poiCount} POIs • {tourCount} tours • {cachedAudioFileCount} audio • {estimatedSizeMb.ToString("0.0", CultureInfo.InvariantCulture)} MB cache"
            : VisitorSettingsPresentationFormatter.FormatOfflinePackSummary(poiCount, tourCount, cachedAudioFileCount, estimatedSizeMb);

    public static string FormatListeningHistoryHeadline(this VisitorUiText text, int entryCount, int completedCount) =>
        text.UsesEnglishUi()
            ? entryCount == 0 ? "No listening history yet" : $"{entryCount} plays • {completedCount} completed"
            : VisitorSettingsPresentationFormatter.FormatListeningHistoryHeadline(entryCount, completedCount);

    public static string SupportedLanguagesLabel(this VisitorUiText text) =>
        text.Pick("Supported languages", "Ngôn ngữ hỗ trợ");

    public static string AboutBuildLabel(this VisitorUiText text) =>
        text.Pick("Anonymous-first mobile build", "Build mobile anonymous-first");

    public static IReadOnlyList<VisitorAboutLinkItem> AboutLinks(this VisitorUiText text) =>
    [
        new(
            text.Pick("Terms of use", "Điều khoản sử dụng"),
            text.Pick("Open the web portal or help center for details.", "Mở bản web hoặc help center để xem chi tiết.")),
        new(
            text.Pick("Privacy policy", "Chính sách quyền riêng tư"),
            text.Pick("Explains how the app uses location and audio history.", "Giải thích cách app dùng vị trí và audio history.")),
        new(
            text.Pick("Open-source licenses", "Giấy phép mã nguồn mở"),
            text.Pick("Package list and licenses currently used.", "Danh sách package và giấy phép đang dùng.")),
        new(
            text.Pick("Send feedback", "Gửi phản hồi"),
            text.Pick("Use this to report bugs or suggest visitor improvements.", "Dùng khi cần báo bug hoặc góp ý bản visitor."))
    ];

    public static string FormatAboutFeedback(this VisitorUiText text, string label) =>
        text.UsesEnglishUi()
            ? $"{label} will open the web/support flow in the next polishing pass."
            : $"{label} sẽ nối sang web/support ở lượt hoàn thiện tiếp theo.";

    public static string NarrationLanguageLabel(this VisitorUiText text) =>
        text.Pick("Narration language", "Ngôn ngữ thuyết minh");

    public static string CalculatingDistanceLabel(this VisitorUiText text) =>
        text.Pick("Calculating", "Đang tính");

    public static string FormatNotPlayedPriority(this VisitorUiText text, string languageLabel) =>
        text.UsesEnglishUi() ? $"Not played • preferred {languageLabel}" : $"Chưa phát • ưu tiên {languageLabel}";

    public static string FormatAutoAudioStatus(this VisitorUiText text, bool autoPlayEnabled, VisitorProximityMatch? activeProximity) =>
        !autoPlayEnabled
            ? text.Pick("Autoplay audio is off", "Audio tự động đang tắt")
            : activeProximity is null
                ? text.Pick("Autoplay audio is ready", "Audio tự động sẵn sàng")
                : text.Pick($"Entered {activeProximity.PoiName} radius", $"Đã vào vùng {activeProximity.PoiName}");

    public static string FormatGeofenceToastMessage(this VisitorUiText text, bool autoPlayEnabled, VisitorProximityMatch? activeProximity, bool isAudioPlaying) =>
        !autoPlayEnabled
            ? text.Pick("Autoplay is off. Tap a POI to listen manually.", "Bạn đã tắt auto-play. Chạm vào POI để nghe thủ công.")
            : activeProximity is null
                ? text.Pick("Ready for automatic narration.", "Sẵn sàng phát thuyết minh tự động.")
                : isAudioPlaying
                    ? text.Pick($"Auto-playing narration • {activeProximity.DistanceMeters}m", $"Đang phát thuyết minh tự động • {activeProximity.DistanceMeters}m")
                    : text.Pick($"Ready to narrate • {activeProximity.DistanceMeters}m", $"Sẵn sàng phát thuyết minh • {activeProximity.DistanceMeters}m");

    public static string? FormatGeofenceQueueBadge(this VisitorUiText text, VisitorProximityMatch? queuedMatch) =>
        queuedMatch is null ? null : text.Pick($"Queued: {queuedMatch.PoiName}", $"Chờ: {queuedMatch.PoiName}");

    public static string FormatPlayingLabel(this VisitorUiText text, string languageCode) =>
        text.UsesEnglishUi() ? $"Playing • {languageCode.ToUpperInvariant()}" : $"Đang phát • {languageCode.ToUpperInvariant()}";

    public static string AudioPlaybackFailedLabel(this VisitorUiText text) =>
        text.Pick("Audio playback failed", "Phát audio thất bại");

    public static string AudioPausedLabel(this VisitorUiText text) =>
        text.Pick("Paused", "Đã tạm dừng");

    public static string AudioNotReadyLabel(this VisitorUiText text) =>
        text.Pick("Audio is not ready", "Audio chưa sẵn sàng");

    public static string CheckingAudioLabel(this VisitorUiText text) =>
        text.Pick("Checking audio...", "Đang kiểm tra audio...");

    public static string FormatQrPlayingLabel(this VisitorUiText text, string languageCode) =>
        text.UsesEnglishUi() ? $"Playing from QR • {languageCode.ToUpperInvariant()}" : $"Đang phát từ QR • {languageCode.ToUpperInvariant()}";

    public static string FormatAutoPlayingLabel(this VisitorUiText text, string languageCode) =>
        text.UsesEnglishUi() ? $"Auto-playing • {languageCode.ToUpperInvariant()}" : $"Đang phát tự động • {languageCode.ToUpperInvariant()}";

    public static string LeftAutoPlayZoneLabel(this VisitorUiText text) =>
        text.Pick("Left the autoplay zone", "Đã rời vùng phát tự động");

    public static string LocalizeKnownStatus(this VisitorUiText text, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !text.UsesEnglishUi())
        {
            return value ?? string.Empty;
        }

        var result = value
            .Replace("Sẵn sàng phát offline", "Ready offline", StringComparison.OrdinalIgnoreCase)
            .Replace("Sẵn sàng phát", "Ready to play", StringComparison.OrdinalIgnoreCase)
            .Replace("Audio demo chưa gắn asset thật.", "Demo audio has no real asset yet.", StringComparison.OrdinalIgnoreCase)
            .Replace("Không tải được audio:", "Could not load audio:", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa có audio sẵn sàng cho ngôn ngữ", "No ready audio for language", StringComparison.OrdinalIgnoreCase)
            .Replace("Audio guide sẵn sàng", "Audio guide ready", StringComparison.OrdinalIgnoreCase)
            .Replace("TTS sẵn sàng", "TTS ready", StringComparison.OrdinalIgnoreCase)
            .Replace("Có tour liên kết", "Linked tour", StringComparison.OrdinalIgnoreCase)
            .Replace("Tạo audio tự động", "Auto-generate audio", StringComparison.OrdinalIgnoreCase)
            .Replace("Đang phát gần đây", "Recently played", StringComparison.OrdinalIgnoreCase)
            .Replace("Sẵn sàng", "Ready", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa bắt đầu", "Not started", StringComparison.OrdinalIgnoreCase)
            .Replace("Hoàn thành", "Completed", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã hoàn thành", "Completed", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã đi qua", "Visited", StringComparison.OrdinalIgnoreCase)
            .Replace("Điểm kế tiếp", "Next stop", StringComparison.OrdinalIgnoreCase)
            .Replace("Chờ tiếp tục", "Waiting", StringComparison.OrdinalIgnoreCase)
            .Replace("Đang cập nhật", "Updating", StringComparison.OrdinalIgnoreCase)
            .Replace("Cập nhật", "Updated", StringComparison.OrdinalIgnoreCase)
            .Replace("Hôm nay", "Today", StringComparison.OrdinalIgnoreCase)
            .Replace("Đang dùng dữ liệu offline đã lưu trên máy.", "Using saved offline data.", StringComparison.OrdinalIgnoreCase)
            .Replace("Không chạm được API thật.", "Could not reach the live API.", StringComparison.OrdinalIgnoreCase)
            .Replace("Không lấy được vị trí:", "Could not get location:", StringComparison.OrdinalIgnoreCase)
            .Replace("Không có POI gần vị trí hiện tại.", "No POIs near your current location.", StringComparison.OrdinalIgnoreCase)
            .Replace("Đang hiển thị toàn bộ", "Showing all", StringComparison.OrdinalIgnoreCase)
            .Replace("POI từ máy chủ", "POIs from the server", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã tải", "Loaded", StringComparison.OrdinalIgnoreCase)
            .Replace("POI gần bạn và", "nearby POIs and", StringComparison.OrdinalIgnoreCase)
            .Replace("tour từ máy chủ", "tours from the server", StringComparison.OrdinalIgnoreCase)
            .Replace("POI và", "POIs and", StringComparison.OrdinalIgnoreCase)
            .Replace("Thiết bị không hỗ trợ định vị", "This device does not support location", StringComparison.OrdinalIgnoreCase)
            .Replace("Không truy cập được vị trí", "Cannot access location", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa cấp quyền vị trí", "Location permission not granted", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã bật quyền, chưa lấy được tọa độ", "Permission granted, no coordinates yet", StringComparison.OrdinalIgnoreCase)
            .Replace("Vị trí gần nhất", "Last known location", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã định vị", "Location acquired", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa bật vị trí", "Location off", StringComparison.OrdinalIgnoreCase)
            .Replace("Dễ đi bộ", "Easy walk", StringComparison.OrdinalIgnoreCase)
            .Replace("Ngon và gần", "Good and nearby", StringComparison.OrdinalIgnoreCase)
            .Replace("Buổi tối", "Evening", StringComparison.OrdinalIgnoreCase)
            .Replace("Khám phá", "Explore", StringComparison.OrdinalIgnoreCase)
            .Replace("điểm dừng", "stops", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếp theo nếu còn đứng trong vùng:", "Next if you stay in range:", StringComparison.OrdinalIgnoreCase)
            .Replace("ưu tiên", "priority", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa có vị trí hiện tại để vẽ đường đi bộ.", "No current location to draw walking directions.", StringComparison.OrdinalIgnoreCase)
            .Replace("Chưa có Mapbox token để vẽ đường đi trong app.", "Missing Mapbox token for in-app directions.", StringComparison.OrdinalIgnoreCase)
            .Replace("Không vẽ được đường đi bộ trong app lúc này.", "Could not draw an in-app walking route right now.", StringComparison.OrdinalIgnoreCase)
            .Replace("Đã có sẵn", "Already cached", StringComparison.OrdinalIgnoreCase)
            .Replace("file trong cache", "files in cache", StringComparison.OrdinalIgnoreCase)
            .Replace("Không có audio phù hợp để tải trước.", "No matching audio to preload.", StringComparison.OrdinalIgnoreCase)
            .Replace("Đang tải", "Downloading", StringComparison.OrdinalIgnoreCase)
            .Replace("Bỏ qua", "Skipped", StringComparison.OrdinalIgnoreCase)
            .Replace("file đã có", "already cached files", StringComparison.OrdinalIgnoreCase)
            .Replace("ghi âm", "recorded", StringComparison.OrdinalIgnoreCase)
            .Replace("ngôn ngữ thuyết minh", "narration languages", StringComparison.OrdinalIgnoreCase)
            .Replace("ngôn ngữ", "languages", StringComparison.OrdinalIgnoreCase)
            .Replace("Đi bộ", "Walk", StringComparison.OrdinalIgnoreCase)
            .Replace("khoảng", "about", StringComparison.OrdinalIgnoreCase)
            .Replace("phút", "min", StringComparison.OrdinalIgnoreCase)
            .Replace("giờ", "hr", StringComparison.OrdinalIgnoreCase);

        return LocalizeLanguageLabel(text, result);
    }

    public static string LocalizeLanguageLabel(this VisitorUiText text, string value)
    {
        if (!text.UsesEnglishUi())
        {
            return value;
        }

        return value
            .Replace("Tiếng Việt", "Vietnamese", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếng Anh", "English", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếng Nhật", "Japanese", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếng Hàn", "Korean", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếng Trung", "Chinese", StringComparison.OrdinalIgnoreCase)
            .Replace("Tiếng Pháp", "French", StringComparison.OrdinalIgnoreCase)
            .Replace("Mặc định", "Default", StringComparison.OrdinalIgnoreCase);
    }

    public static string LocalizeCategoryLabel(this VisitorUiText text, string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || !text.UsesEnglishUi())
        {
            return label ?? string.Empty;
        }

        return RemoveDiacritics(label).ToLowerInvariant() switch
        {
            "tat ca" => "All",
            "hai san" => "Seafood",
            "bun/pho" or "bun pho" => "Noodles/Pho",
            "am thuc" => "Food",
            "lich su" or "di tich" => "History",
            "ven song" => "Riverside",
            "dem" => "Night",
            _ => label
        };
    }

    public static string FormatStopCountLabel(this VisitorUiText text, string stopCountLabel)
    {
        if (!text.UsesEnglishUi())
        {
            return stopCountLabel;
        }

        var number = new string(stopCountLabel.TakeWhile(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(number) ? stopCountLabel : $"{number} stops";
    }

    public static string FormatProgressPointLabel(this VisitorUiText text, int completed, int total) =>
        text.UsesEnglishUi() ? $"{completed}/{total} stops" : $"{completed}/{total} điểm";

    public static string FormatTourActionLabel(this VisitorUiText text, VisitorTourSession? activeSession, string tourId)
    {
        if (!text.UsesEnglishUi())
        {
            return VisitorTourPresentationFormatter.GetActionLabel(activeSession, tourId);
        }

        if (activeSession?.TourId == tourId)
        {
            return activeSession.IsCompleted ? "Replay tour" : "Continue tour";
        }

        return "Start tour";
    }

    public static string FormatSelectedTourActionLabel(this VisitorUiText text, VisitorTourCard? selectedTour, VisitorTourSession? activeSession) =>
        selectedTour is null ? text.Pick("Start tour", "Bắt đầu tour") : text.FormatTourActionLabel(activeSession, selectedTour.Id);

    public static string FormatTourProgressLabel(this VisitorUiText text, VisitorTourCard? selectedTour, VisitorTourSession? activeSession) =>
        activeSession is null || activeSession.TourId != selectedTour?.Id
            ? text.FormatProgressPointLabel(0, selectedTour?.StopPoiIds.Count ?? 0)
            : text.FormatProgressPointLabel(activeSession.CurrentStopSequence, activeSession.TotalStops);

    public static string FormatTourStopStateLabel(this VisitorUiText text, VisitorTourSession? activeSession, string tourId, string poiId, int stopIndex)
    {
        if (!text.UsesEnglishUi())
        {
            return VisitorTourPresentationFormatter.GetStopStateLabel(activeSession, tourId, poiId, stopIndex);
        }

        if (activeSession?.TourId != tourId)
        {
            return "Ready";
        }

        if (activeSession.IsCompleted)
        {
            return "Completed";
        }

        if (stopIndex < activeSession.CurrentStopSequence)
        {
            return "Visited";
        }

        return activeSession.NextPoiId == poiId ? "Next stop" : "Waiting";
    }

    public static string FormatDurationLabel(this VisitorUiText text, int minutes)
    {
        var safeMinutes = Math.Max(1, minutes);
        if (!text.UsesEnglishUi())
        {
            return safeMinutes < 60
                ? $"{safeMinutes} phút"
                : safeMinutes % 60 == 0
                    ? $"{safeMinutes / 60} giờ"
                    : $"{safeMinutes / 60} giờ {safeMinutes % 60} phút";
        }

        return safeMinutes < 60
            ? $"{safeMinutes} min"
            : safeMinutes % 60 == 0
                ? $"{safeMinutes / 60} hr"
                : $"{safeMinutes / 60} hr {safeMinutes % 60} min";
    }

    public static string FormatWalkingStatus(this VisitorUiText text, int distanceMeters, int durationMinutes) =>
        text.UsesEnglishUi()
            ? $"Walk {VisitorWalkingDirectionsPresentationFormatter.FormatDistance(distanceMeters)} • about {text.FormatDurationLabel(durationMinutes)}"
            : $"Đi bộ {VisitorWalkingDirectionsPresentationFormatter.FormatDistance(distanceMeters)} • khoảng {text.FormatDurationLabel(durationMinutes)}";

    public static string WalkingLoadingStatus(this VisitorUiText text) =>
        text.Pick("Drawing walking route in the app...", "Đang vẽ đường đi bộ trong app...");

    public static string FormatDirectionsTitle(this VisitorUiText text, string poiName, bool isLoading) =>
        isLoading
            ? text.Pick($"Finding route to {poiName}", $"Đang tìm đường tới {poiName}")
            : text.Pick($"Going to {poiName}", $"Dẫn tới {poiName}");

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character == 'đ' ? 'd' : character == 'Đ' ? 'D' : character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
