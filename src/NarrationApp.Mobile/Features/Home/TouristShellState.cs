using System.Globalization;
using System.Text;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public enum TouristIntroStep
{
    Welcome,
    Language,
    Permissions,
    Ready
}

public enum TouristTab
{
    Map,
    Discover,
    Tours,
    Settings
}

public sealed class TouristShellState
{
    private readonly List<TouristLanguageOption> _languages;
    private readonly List<TouristCategory> _categories;
    private readonly List<TouristPoi> _pois;
    private readonly List<TouristNotification> _notifications;
    private readonly List<TouristTourCard> _tours;
    private readonly List<TouristCachedAudioItem> _cachedAudioItems = [];
    private readonly List<TouristListeningHistoryDay> _listeningHistoryDays = [];

    private TouristShellState(
        List<TouristLanguageOption> languages,
        List<TouristCategory> categories,
        List<TouristPoi> pois,
        List<TouristNotification> notifications,
        List<TouristTourCard> tours)
    {
        _languages = languages;
        _categories = categories;
        _pois = pois;
        _notifications = notifications;
        _tours = tours;
    }

    public TouristIntroStep CurrentStep { get; private set; } = TouristIntroStep.Welcome;

    public TouristTab CurrentTab { get; private set; } = TouristTab.Map;

    public TouristSettingsScreen CurrentSettingsScreen { get; private set; } = TouristSettingsScreen.Overview;

    public bool IsGuestMode { get; } = true;

    public string SelectedLanguageCode { get; private set; } = "vi";

    public bool LocationPermissionGranted { get; private set; }

    public string SelectedCategoryId { get; private set; } = "all";

    public string SearchTerm { get; private set; } = string.Empty;

    public bool ShowNotifications { get; private set; }

    public bool ShowPoiSheet { get; private set; }

    public bool ShowMiniPlayer { get; private set; }

    public string? SelectedPoiId { get; private set; }

    public string? SelectedTourId { get; private set; }

    public TouristLocationSnapshot CurrentLocation { get; private set; } = TouristLocationSnapshot.Disabled();

    public string LocationStatusLabel { get; private set; } = "Chưa bật vị trí";

    public string DataSourceLabel { get; private set; } = "Demo fallback";

    public string SyncMessage { get; private set; } = "Đang dùng dữ liệu demo cục bộ.";

    public bool IsUsingFallbackData { get; private set; } = true;

    public TouristProximityMatch? ActiveProximity { get; private set; }

    public string AutoNarrationPrompt { get; private set; } = "Chưa có gợi ý phát tự động.";

    public bool HasAutoNarrationPrompt => ActiveProximity is not null;

    public TouristAudioCue? CurrentAudioCue { get; private set; }

    public string AudioStatusLabel { get; private set; } = "Chưa nạp audio.";

    public bool CanPlayAudio => CurrentAudioCue?.IsAvailable == true;

    public TouristAudioPlaybackState AudioPlaybackState { get; private set; } = TouristAudioPlaybackState.Idle;

    public bool IsAudioPlaying => AudioPlaybackState == TouristAudioPlaybackState.Playing;

    public int AudioElapsedSeconds { get; private set; }

    public int AudioDurationSeconds { get; private set; }

    public double AudioProgressPercent =>
        AudioDurationSeconds <= 0
            ? 0d
            : Math.Clamp(AudioElapsedSeconds * 100d / AudioDurationSeconds, 0d, 100d);

    public string AudioElapsedLabel => FormatDuration(AudioElapsedSeconds);

    public string AudioDurationLabel => FormatDuration(AudioDurationSeconds);

    public IReadOnlyList<TouristLanguageOption> Languages => _languages;

    public IReadOnlyList<TouristCategory> Categories => _categories;

    public IReadOnlyList<TouristPoi> Pois => _pois;

    public IReadOnlyList<TouristNotification> Notifications => _notifications;

    public IReadOnlyList<TouristTourCard> Tours => _tours;

    public TouristLanguageOption CurrentLanguage => _languages.First(language => language.Code == SelectedLanguageCode);

    public TouristPoi? SelectedPoi => _pois.FirstOrDefault(poi => poi.Id == SelectedPoiId);

    public TouristTourCard? SelectedTour => _tours.FirstOrDefault(tour => tour.Id == SelectedTourId);

    public TouristTourSession? ActiveTourSession { get; private set; }

    public TouristAudioPreferences AudioPreferences { get; private set; } = new(
        AutoPlayEnabled: true,
        SpokenAnnouncementsEnabled: true,
        AutoAdvanceEnabled: false,
        SourcePreference: TouristAudioSourcePreference.RecordedFirst,
        DefaultPlaybackSpeed: 1d,
        CooldownLabel: "Cooldown geofence 45 giây",
        QueueLabel: "Ưu tiên 1 POI tại mỗi lần tự phát");

    public TouristGpsPreferences GpsPreferences { get; private set; } = new(
        BackgroundTrackingEnabled: true,
        AutoFocusEnabled: true,
        AccuracyMode: TouristGpsAccuracyMode.Adaptive,
        BatteryPercent: 82,
        StatusLabel: "GPS đang bật và sẵn sàng geofence",
        BatteryLabel: "Adaptive mode • tiết kiệm pin");

    public IReadOnlyList<TouristCachedAudioItem> CachedAudioItems => _cachedAudioItems;

    public IReadOnlyList<TouristListeningHistoryDay> ListeningHistoryDays => _listeningHistoryDays;

    public IReadOnlyList<TouristPoi> FilteredPois =>
        _pois
            .Where(poi => SelectedCategoryId == "all" || poi.CategoryId == SelectedCategoryId)
            .Where(MatchesSearch)
            .ToList();

    public IReadOnlyList<TouristPoi> FeaturedPois =>
        FilteredPois
            .OrderBy(poi => poi.DistanceMeters)
            .Take(5)
            .ToList();

    public static TouristShellState CreateDefault()
    {
        var state = new TouristShellState(
            languages:
            [
                new TouristLanguageOption("vi", "Tiếng Việt", "Nguồn chuẩn", "VN"),
                new TouristLanguageOption("en", "English", "English guide", "EN"),
                new TouristLanguageOption("ja", "日本語", "Japanese guide", "JP"),
                new TouristLanguageOption("ko", "한국어", "Korean guide", "KR"),
                new TouristLanguageOption("zh", "中文", "Chinese guide", "ZH"),
                new TouristLanguageOption("fr", "Français", "French guide", "FR")
            ],
            categories:
            [
                new TouristCategory("all", "Tất cả", "◌"),
                new TouristCategory("food", "Ẩm thực", "●"),
                new TouristCategory("history", "Lịch sử", "▲"),
                new TouristCategory("river", "Ven sông", "■"),
                new TouristCategory("night", "Đêm", "✦")
            ],
            pois: [],
            notifications:
            [
                new TouristNotification("Đang ở gần Cầu Khánh Hội", "Bật audio tự động để nghe khi tới geofence.", "2 phút trước"),
                new TouristNotification("Tour mới vừa mở", "Khám phá tuyến Ven sông Khánh Hội có 4 điểm dừng.", "10 phút trước"),
                new TouristNotification("Ngôn ngữ English sẵn sàng", "Bạn có thể đổi ngôn ngữ ở header bất kỳ lúc nào.", "Hôm nay")
            ],
            tours: []);

        state.ApplyContent(TouristContentSnapshot.CreateDemo());
        state.SeedSettingsDemoData();
        return state;
    }

    public void ApplyContent(TouristContentSnapshot snapshot, bool isFallback = true, string? sourceLabel = null, string? syncMessage = null)
    {
        _pois.Clear();
        _pois.AddRange(snapshot.Pois);

        _tours.Clear();
        _tours.AddRange(snapshot.Tours);

        IsUsingFallbackData = isFallback;
        DataSourceLabel = sourceLabel ?? (isFallback ? "Demo fallback" : "Live API");
        SyncMessage = syncMessage ?? (isFallback ? "Đang dùng dữ liệu demo cục bộ." : "Đã đồng bộ dữ liệu từ máy chủ.");

        EnsureSelectedPoiStillVisible();
        EnsureSelectedTourStillVisible();
        EnsureActiveTourStillVisible();
    }

    public void UpdateLocation(TouristLocationSnapshot location)
    {
        CurrentLocation = location;
        LocationPermissionGranted = location.PermissionGranted;
        LocationStatusLabel = location.StatusLabel;
    }

    public void ApplyProximityFocus(TouristProximityMatch? proximity)
    {
        ActiveProximity = proximity;

        if (proximity is null)
        {
            AutoNarrationPrompt = "Chưa ở trong vùng phát tự động.";
            return;
        }

        AutoNarrationPrompt = $"Bạn đang ở gần {proximity.PoiName} ({proximity.DistanceMeters}m). Sẵn sàng phát audio tự động.";
        OpenPoi(proximity.PoiId);
    }

    public void SetAudioCue(TouristAudioCue cue)
    {
        CurrentAudioCue = cue;
        AudioStatusLabel = cue.StatusLabel;
        AudioElapsedSeconds = 0;
        AudioDurationSeconds = cue.DurationSeconds;
        AudioPlaybackState = cue.IsAvailable ? TouristAudioPlaybackState.Ready : TouristAudioPlaybackState.Error;
    }

    public void SetAudioPlaybackState(TouristAudioPlaybackState playbackState, string? statusLabel = null)
    {
        AudioPlaybackState = playbackState;

        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            AudioStatusLabel = statusLabel;
        }
    }

    public void UpdateAudioProgress(int elapsedSeconds, int? durationSeconds = null)
    {
        AudioDurationSeconds = durationSeconds is > 0 ? durationSeconds.Value : AudioDurationSeconds;
        AudioElapsedSeconds = Math.Clamp(elapsedSeconds, 0, Math.Max(AudioDurationSeconds, elapsedSeconds));
    }

    public void ContinueFromWelcome()
    {
        CurrentStep = TouristIntroStep.Language;
    }

    public void SelectLanguage(string languageCode)
    {
        if (!_languages.Any(language => language.Code == languageCode))
        {
            return;
        }

        SelectedLanguageCode = languageCode;
        CurrentStep = TouristIntroStep.Permissions;
    }

    public void ChangeLanguage(string languageCode)
    {
        if (_languages.Any(language => language.Code == languageCode))
        {
            SelectedLanguageCode = languageCode;
        }
    }

    public void CompletePermissions(bool granted)
    {
        LocationPermissionGranted = granted;
        CurrentStep = TouristIntroStep.Ready;

        if (SelectedPoi is null)
        {
            OpenPoi(_pois[0].Id);
        }
    }

    public void EnterReadyFromExternalEntry()
    {
        CurrentStep = TouristIntroStep.Ready;
        ResetDiscoverFilters();
        ShowNotifications = false;

        if (SelectedPoi is not null)
        {
            ShowPoiSheet = true;
            ShowMiniPlayer = true;
            return;
        }

        if (_pois.Count > 0)
        {
            OpenPoi(_pois[0].Id);
        }
    }

    public void ApplyQrNavigationTarget(TouristQrNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        EnterReadyFromExternalEntry();

        switch (target.Kind)
        {
            case TouristQrTargetKind.Poi when !string.IsNullOrWhiteSpace(target.TargetId) && _pois.Any(poi => poi.Id == target.TargetId):
                OpenPoi(target.TargetId);
                return;

            case TouristQrTargetKind.Tour when !string.IsNullOrWhiteSpace(target.TargetId) && _tours.Any(tour => tour.Id == target.TargetId):
                SelectTour(target.TargetId);
                return;

            default:
                SwitchTab(TouristTab.Map);
                return;
        }
    }

    public void SwitchTab(TouristTab tab)
    {
        CurrentTab = tab;

        if (tab != TouristTab.Settings)
        {
            CurrentSettingsScreen = TouristSettingsScreen.Overview;
        }

        if (tab == TouristTab.Map && SelectedPoi is null && _pois.Count > 0)
        {
            OpenPoi(_pois[0].Id);
        }

        if (tab == TouristTab.Tours && SelectedTour is null && _tours.Count > 0)
        {
            SelectedTourId = _tours[0].Id;
        }
    }

    public void OpenSettingsScreen(TouristSettingsScreen screen)
    {
        CurrentTab = TouristTab.Settings;
        CurrentSettingsScreen = screen;
    }

    public void CloseSettingsScreen()
    {
        CurrentSettingsScreen = TouristSettingsScreen.Overview;
    }

    public void SelectCategory(string categoryId)
    {
        if (!_categories.Any(category => category.Id == categoryId))
        {
            return;
        }

        SelectedCategoryId = categoryId;
        EnsureSelectedPoiStillVisible();
    }

    public void SetSearchTerm(string searchTerm)
    {
        SearchTerm = searchTerm.Trim();
        EnsureSelectedPoiStillVisible();
    }

    public void ToggleNotifications()
    {
        ShowNotifications = !ShowNotifications;
    }

    public void SetAudioAutoPlayEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { AutoPlayEnabled = isEnabled };
    }

    public void SetAudioSpokenAnnouncementsEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { SpokenAnnouncementsEnabled = isEnabled };
    }

    public void SetAudioAutoAdvanceEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { AutoAdvanceEnabled = isEnabled };
    }

    public void SetAudioSourcePreference(TouristAudioSourcePreference preference)
    {
        AudioPreferences = AudioPreferences with { SourcePreference = preference };
    }

    public void SetAudioPlaybackSpeed(double speed)
    {
        AudioPreferences = AudioPreferences with { DefaultPlaybackSpeed = speed };
    }

    public void SetGpsBackgroundTrackingEnabled(bool isEnabled)
    {
        GpsPreferences = GpsPreferences with { BackgroundTrackingEnabled = isEnabled };
    }

    public void SetGpsAutoFocusEnabled(bool isEnabled)
    {
        GpsPreferences = GpsPreferences with { AutoFocusEnabled = isEnabled };
    }

    public void SetGpsAccuracyMode(TouristGpsAccuracyMode mode)
    {
        var batteryLabel = mode switch
        {
            TouristGpsAccuracyMode.High => "High accuracy • pin giảm nhanh hơn",
            TouristGpsAccuracyMode.BatterySaver => "Battery saver • giảm tần suất GPS",
            _ => "Adaptive mode • tiết kiệm pin"
        };

        GpsPreferences = GpsPreferences with
        {
            AccuracyMode = mode,
            BatteryLabel = batteryLabel
        };
    }

    public void RemoveCachedAudioItem(string itemId)
    {
        var index = _cachedAudioItems.FindIndex(item => item.Id == itemId);
        if (index >= 0)
        {
            _cachedAudioItems.RemoveAt(index);
        }
    }

    public void ClearCachedAudioItems()
    {
        _cachedAudioItems.Clear();
    }

    public void OpenPoi(string poiId)
    {
        if (!_pois.Any(poi => poi.Id == poiId))
        {
            return;
        }

        SelectedPoiId = poiId;
        ShowPoiSheet = true;
        ShowMiniPlayer = true;
        CurrentTab = TouristTab.Map;
        CurrentSettingsScreen = TouristSettingsScreen.Overview;
    }

    public void PreviewPoi(string poiId)
    {
        if (!_pois.Any(poi => poi.Id == poiId))
        {
            return;
        }

        SelectedPoiId = poiId;
        ShowPoiSheet = false;
        ShowMiniPlayer = true;
        CurrentSettingsScreen = TouristSettingsScreen.Overview;
    }

    public void SelectTour(string tourId)
    {
        if (_tours.Any(tour => tour.Id == tourId))
        {
            SelectedTourId = tourId;
            CurrentTab = TouristTab.Tours;
            CurrentSettingsScreen = TouristSettingsScreen.Overview;
        }
    }

    public void StartTour(string tourId)
    {
        var tour = _tours.FirstOrDefault(item => item.Id == tourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            return;
        }

        SelectedTourId = tour.Id;
        var nextPoiId = tour.StopPoiIds[0];
        ActiveTourSession = new TouristTourSession(
            TourId: tour.Id,
            TourTitle: tour.Title,
            CurrentStopSequence: 0,
            TotalStops: tour.StopPoiIds.Count,
            NextPoiId: nextPoiId,
            NextPoiName: ResolvePoiName(nextPoiId),
            IsCompleted: false);

        OpenPoi(nextPoiId);
    }

    public void ApplyServerTourSession(TouristTourSession session)
    {
        ActiveTourSession = session;
        SelectedTourId = session.TourId;

        if (!string.IsNullOrWhiteSpace(session.NextPoiId))
        {
            OpenPoi(session.NextPoiId);
            return;
        }

        CurrentTab = TouristTab.Tours;
        CurrentSettingsScreen = TouristSettingsScreen.Overview;
    }

    public void ClearActiveTourSession()
    {
        ActiveTourSession = null;
    }

    public bool AdvanceActiveTour(string poiId)
    {
        if (ActiveTourSession is null)
        {
            return false;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == ActiveTourSession.TourId);
        if (tour is null || ActiveTourSession.CurrentStopSequence >= tour.StopPoiIds.Count)
        {
            return false;
        }

        var expectedPoiId = tour.StopPoiIds[ActiveTourSession.CurrentStopSequence];
        if (!string.Equals(expectedPoiId, poiId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextSequence = ActiveTourSession.CurrentStopSequence + 1;
        if (nextSequence >= tour.StopPoiIds.Count)
        {
            ActiveTourSession = ActiveTourSession with
            {
                CurrentStopSequence = nextSequence,
                NextPoiId = null,
                NextPoiName = "Hoàn thành",
                IsCompleted = true
            };

            return true;
        }

        var nextPoiId = tour.StopPoiIds[nextSequence];
        ActiveTourSession = ActiveTourSession with
        {
            CurrentStopSequence = nextSequence,
            NextPoiId = nextPoiId,
            NextPoiName = ResolvePoiName(nextPoiId),
            IsCompleted = false
        };

        OpenPoi(nextPoiId);
        return true;
    }

    public void ClosePoiSheet()
    {
        ShowPoiSheet = false;
    }

    public void ToggleMiniPlayer()
    {
        ShowMiniPlayer = !ShowMiniPlayer;
    }

    private void EnsureSelectedPoiStillVisible()
    {
        var visiblePois = FilteredPois;
        if (visiblePois.Count == 0)
        {
            SelectedPoiId = null;
            ShowPoiSheet = false;
            ShowMiniPlayer = false;
            return;
        }

        if (SelectedPoiId is not null && visiblePois.Any(poi => poi.Id == SelectedPoiId))
        {
            return;
        }

        SelectedPoiId = visiblePois[0].Id;
        ShowPoiSheet = true;
        ShowMiniPlayer = true;
    }

    private void EnsureSelectedTourStillVisible()
    {
        if (_tours.Count == 0)
        {
            SelectedTourId = null;
            return;
        }

        if (SelectedTourId is not null && _tours.Any(tour => tour.Id == SelectedTourId))
        {
            return;
        }

        SelectedTourId = _tours[0].Id;
    }

    private void EnsureActiveTourStillVisible()
    {
        if (ActiveTourSession is null)
        {
            return;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == ActiveTourSession.TourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            ActiveTourSession = null;
            return;
        }

        if (ActiveTourSession.IsCompleted)
        {
            return;
        }

        var nextIndex = Math.Clamp(ActiveTourSession.CurrentStopSequence, 0, tour.StopPoiIds.Count - 1);
        var nextPoiId = tour.StopPoiIds[nextIndex];
        ActiveTourSession = ActiveTourSession with
        {
            TotalStops = tour.StopPoiIds.Count,
            NextPoiId = nextPoiId,
            NextPoiName = ResolvePoiName(nextPoiId)
        };
    }

    private void SeedSettingsDemoData()
    {
        if (_pois.Count == 0)
        {
            return;
        }

        _cachedAudioItems.Clear();
        _cachedAudioItems.AddRange(
        [
            new TouristCachedAudioItem("cache-poi-khanh-hoi-vi", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "vi", "Recorded", 6.4, "Cập nhật 12 phút trước"),
            new TouristCachedAudioItem("cache-poi-khanh-hoi-en", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "en", "Google TTS", 4.1, "Cập nhật 12 phút trước"),
            new TouristCachedAudioItem("cache-poi-ben-nha-rong-vi", "poi-ben-nha-rong", "Bến Nhà Rồng", "vi", "Recorded", 5.8, "Cập nhật 1 giờ trước"),
            new TouristCachedAudioItem("cache-poi-ben-nha-rong-ja", "poi-ben-nha-rong", "Bến Nhà Rồng", "ja", "Google TTS", 4.6, "Cập nhật 1 giờ trước"),
            new TouristCachedAudioItem("cache-poi-cho-ben-thanh-zh", "poi-cho-ben-thanh", "Chợ Bến Thành", "zh", "Google TTS", 4.9, "Cập nhật hôm nay"),
            new TouristCachedAudioItem("cache-poi-pho-dem-fr", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "fr", "Google TTS", 3.9, "Cập nhật hôm nay")
        ]);

        _listeningHistoryDays.Clear();
        _listeningHistoryDays.AddRange(
        [
            new TouristListeningHistoryDay(
                "Hôm nay",
                [
                    new TouristListeningHistoryEntry("history-1", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "Ven sông", "vi", "08:42", "3:12", 100),
                    new TouristListeningHistoryEntry("history-2", "poi-ben-nha-rong", "Bến Nhà Rồng", "Lịch sử", "en", "09:15", "2:40", 76),
                    new TouristListeningHistoryEntry("history-3", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "Đêm", "fr", "11:30", "2:18", 42)
                ]),
            new TouristListeningHistoryDay(
                "Hôm qua",
                [
                    new TouristListeningHistoryEntry("history-4", "poi-cho-ben-thanh", "Chợ Bến Thành", "Ẩm thực", "zh", "17:22", "3:05", 100),
                    new TouristListeningHistoryEntry("history-5", "poi-tiem-banh-mi-co-lan", "Tiệm Bánh Mì Cô Lan", "Bánh mì", "vi", "18:04", "2:12", 63)
                ])
        ]);
    }

    private void ResetDiscoverFilters()
    {
        SelectedCategoryId = "all";
        SearchTerm = string.Empty;
    }

    private string ResolvePoiName(string poiId)
    {
        return _pois.FirstOrDefault(poi => poi.Id == poiId)?.Name ?? "POI kế tiếp";
    }

    private bool MatchesSearch(TouristPoi poi)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return true;
        }

        var searchTerm = Normalize(SearchTerm);
        return Normalize(poi.Name).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || Normalize(poi.StoryTag).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || Normalize(poi.District).Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string FormatDuration(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:00}";
    }
}

public sealed record TouristLanguageOption(string Code, string Label, string SubLabel, string ChipLabel);

public sealed record TouristCategory(string Id, string Label, string MarkerLabel);

public sealed record TouristPoi(
    string Id,
    string Name,
    string CategoryId,
    string CategoryLabel,
    string District,
    string StoryTag,
    string Description,
    string Highlight,
    double MapTopPercent,
    double MapLeftPercent,
    int DistanceMeters,
    string AudioDuration,
    string StatusLabel,
    double Latitude,
    double Longitude,
    int Priority = 1,
    int AvailableLanguageCount = 1,
    int GeofenceRadiusMeters = 30);

public sealed record TouristNotification(string Title, string Body, string TimeLabel);

public sealed record TouristTourCard(
    string Id,
    string Title,
    string StopCountLabel,
    string DurationLabel,
    string DifficultyLabel,
    string Description,
    IReadOnlyList<string> StopPoiIds);

public sealed record TouristTourSession(
    string TourId,
    string TourTitle,
    int CurrentStopSequence,
    int TotalStops,
    string? NextPoiId,
    string NextPoiName,
    bool IsCompleted,
    bool IsServerBacked = false,
    TourSessionStatus? SyncStatus = null);
