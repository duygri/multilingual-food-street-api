using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    private readonly List<VisitorLanguageOption> _languages;
    private readonly List<VisitorCategory> _categories;
    private readonly List<VisitorPoi> _pois;
    private readonly List<VisitorNotification> _notifications;
    private readonly List<VisitorTourCard> _tours;
    private readonly List<VisitorCachedAudioItem> _cachedAudioItems = [];
    private readonly List<VisitorListeningHistoryDay> _listeningHistoryDays = [];
    private IReadOnlyList<VisitorPoi>? _filteredPoisCache;
    private IReadOnlyList<VisitorPoi>? _featuredPoisCache;
    private IReadOnlyList<VisitorPoi>? _discoverPoisCache;
    private IReadOnlyList<VisitorPoi>? _featuredDiscoverPoisCache;
    private VisitorQrNavigationTarget? _pendingQrNavigationTarget;
    private string? _dismissedProximityPoiId;

    private VisitorShellState(
        List<VisitorLanguageOption> languages,
        List<VisitorCategory> categories,
        List<VisitorPoi> pois,
        List<VisitorNotification> notifications,
        List<VisitorTourCard> tours)
    {
        _languages = languages;
        _categories = categories;
        _pois = pois;
        _notifications = notifications;
        _tours = tours;
    }

    public VisitorIntroStep CurrentStep { get; private set; } = VisitorIntroStep.Welcome;

    public VisitorTab CurrentTab { get; private set; } = VisitorTab.Discover;

    public VisitorSettingsScreen CurrentSettingsScreen { get; private set; } = VisitorSettingsScreen.Overview;

    public string SelectedLanguageCode { get; private set; } = "vi";

    public string SelectedAppLanguageCode { get; private set; } = "vi";

    public bool LocationPermissionGranted { get; private set; }

    public string SelectedCategoryId { get; private set; } = "all";

    public string SearchTerm { get; private set; } = string.Empty;

    public bool ShowNotifications { get; private set; }

    public bool ShowPoiSheet { get; private set; }

    public bool ShowMiniPlayer { get; private set; }

    public string? SelectedPoiId { get; private set; }

    public string? SelectedTourId { get; private set; }

    public VisitorLocationSnapshot CurrentLocation { get; private set; } = VisitorLocationSnapshot.Disabled();

    public string LocationStatusLabel { get; private set; } = "Chưa cấp quyền vị trí";

    public string DataSourceLabel { get; private set; } = "Demo fallback";

    public string SyncMessage { get; private set; } = "Đang dùng dữ liệu demo cục bộ.";

    public bool IsUsingFallbackData { get; private set; } = true;

    public VisitorProximityMatch? ActiveProximity { get; private set; }

    public string AutoNarrationPrompt { get; private set; } = "Chưa có gợi ý phát tự động.";

    public bool HasAutoNarrationPrompt => ActiveProximity is not null;

    public VisitorAudioCue? CurrentAudioCue { get; private set; }

    public string AudioStatusLabel { get; private set; } = "Chưa nạp audio.";

    public bool CanPlayAudio => CurrentAudioCue?.IsAvailable == true;

    public VisitorAudioPlaybackState AudioPlaybackState { get; private set; } = VisitorAudioPlaybackState.Idle;

    public bool IsAudioPlaying => AudioPlaybackState == VisitorAudioPlaybackState.Playing;

    public int AudioElapsedSeconds { get; private set; }

    public int AudioDurationSeconds { get; private set; }

    public double AudioProgressPercent =>
        AudioDurationSeconds <= 0
            ? 0d
            : Math.Clamp(AudioElapsedSeconds * 100d / AudioDurationSeconds, 0d, 100d);

    public string AudioElapsedLabel => VisitorDurationFormatter.FormatSeconds(AudioElapsedSeconds);

    public string AudioDurationLabel => VisitorDurationFormatter.FormatSeconds(AudioDurationSeconds);

    public IReadOnlyList<VisitorLanguageOption> Languages => _languages;

    public IReadOnlyList<VisitorLanguageOption> AppLanguages =>
        VisitorLanguageCatalog.Defaults
            .Where(language => VisitorUiTextCatalog.IsSupportedAppLanguage(language.Code))
            .ToArray();

    public IReadOnlyList<VisitorCategory> Categories => _categories;

    public IReadOnlyList<VisitorPoi> Pois => _pois;

    public IReadOnlyList<VisitorNotification> Notifications => _notifications;

    public IReadOnlyList<VisitorTourCard> Tours => _tours;

    public VisitorLanguageOption CurrentLanguage => _languages.First(language => language.Code == SelectedLanguageCode);

    public VisitorLanguageOption CurrentAppLanguage =>
        AppLanguages.First(language => string.Equals(language.Code, SelectedAppLanguageCode, StringComparison.OrdinalIgnoreCase));

    public VisitorPoi? SelectedPoi => _pois.FirstOrDefault(poi => poi.Id == SelectedPoiId);

    public VisitorTourCard? SelectedTour => _tours.FirstOrDefault(tour => tour.Id == SelectedTourId);

    public VisitorTourSession? ActiveTourSession { get; private set; }

    public VisitorAudioPreferences AudioPreferences { get; private set; } = new(
        AutoPlayEnabled: true,
        SpokenAnnouncementsEnabled: true,
        AutoAdvanceEnabled: false,
        SourcePreference: VisitorAudioSourcePreference.RecordedFirst,
        DefaultPlaybackSpeed: 1d,
        CooldownLabel: "Cooldown geofence 5 phút",
        QueueLabel: "Overlap ưu tiên priority, đổi POI sau 3 mẫu ổn định");

    public VisitorGpsPreferences GpsPreferences { get; private set; } = new(
        BackgroundTrackingEnabled: true,
        AutoFocusEnabled: true,
        AccuracyMode: VisitorGpsAccuracyMode.Adaptive,
        BatteryPercent: 82,
        StatusLabel: "GPS đang bật và sẵn sàng geofence",
        BatteryLabel: "Adaptive mode • tiết kiệm pin");

    public IReadOnlyList<VisitorCachedAudioItem> CachedAudioItems => _cachedAudioItems;

    public IReadOnlyList<VisitorListeningHistoryDay> ListeningHistoryDays => _listeningHistoryDays;

    public IReadOnlyList<VisitorPoi> FilteredPois =>
        _filteredPoisCache ??= VisitorPoiFilter.Apply(_pois, SelectedCategoryId, SearchTerm);

    public IReadOnlyList<VisitorPoi> FeaturedPois =>
        _featuredPoisCache ??= FilteredPois
            .Take(5)
            .ToArray();

    public IReadOnlyList<VisitorPoi> DiscoverPois =>
        _discoverPoisCache ??= VisitorPoiFilter.WithReadyAudioForLanguage(FilteredPois, SelectedLanguageCode);

    public IReadOnlyList<VisitorPoi> FeaturedDiscoverPois =>
        _featuredDiscoverPoisCache ??= DiscoverPois
            .Take(5)
            .ToArray();

    public static VisitorShellState CreateDefault()
    {
        var state = CreateBaseState(includeDemoNotifications: true);
        state.ApplyContent(VisitorContentSnapshot.CreateDemo());
        state.SeedSettingsDemoData();
        return state;
    }

    public static VisitorShellState CreateRuntimeDefault()
    {
        var state = CreateBaseState(includeDemoNotifications: false);
        state.DataSourceLabel = "Đang chờ đồng bộ";
        state.SyncMessage = "Đang chờ đồng bộ dữ liệu từ máy chủ.";
        state.IsUsingFallbackData = true;
        return state;
    }

    private static VisitorShellState CreateBaseState(bool includeDemoNotifications)
    {
        return new VisitorShellState(
            languages: VisitorLanguageCatalog.Defaults.ToList(),
            categories:
            [
                new VisitorCategory("all", "Tất cả", "map", "is-history")
            ],
            pois: [],
            notifications: includeDemoNotifications
                ? [
                    new VisitorNotification("Đang ở gần Cầu Khánh Hội", "Bật audio tự động để nghe khi tới geofence.", "2 phút trước"),
                    new VisitorNotification("Tour mới vừa mở", "Khám phá tuyến Ven sông Khánh Hội có 4 điểm dừng.", "10 phút trước"),
                    new VisitorNotification("Ngôn ngữ English sẵn sàng", "Bạn có thể đổi ngôn ngữ ở header bất kỳ lúc nào.", "Hôm nay")
                ]
                : [],
            tours: []);
    }

    private string ResolvePoiName(string poiId)
    {
        return _pois.FirstOrDefault(poi => poi.Id == poiId)?.Name ?? "POI kế tiếp";
    }
}
