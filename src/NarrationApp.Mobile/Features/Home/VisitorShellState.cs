using System.Globalization;
using System.Text;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public enum VisitorIntroStep
{
    Welcome,
    Language,
    Permissions,
    Ready
}

public enum VisitorTab
{
    Map,
    Discover,
    Tours,
    Settings
}

public sealed class VisitorShellState
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

    public VisitorIntroStep CurrentStep { get; private set; } = VisitorIntroStep.Language;

    public VisitorTab CurrentTab { get; private set; } = VisitorTab.Map;

    public VisitorSettingsScreen CurrentSettingsScreen { get; private set; } = VisitorSettingsScreen.Overview;

    public string SelectedLanguageCode { get; private set; } = "vi";

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

    public string AudioElapsedLabel => FormatDuration(AudioElapsedSeconds);

    public string AudioDurationLabel => FormatDuration(AudioDurationSeconds);

    public IReadOnlyList<VisitorLanguageOption> Languages => _languages;

    public IReadOnlyList<VisitorCategory> Categories => _categories;

    public IReadOnlyList<VisitorPoi> Pois => _pois;

    public IReadOnlyList<VisitorNotification> Notifications => _notifications;

    public IReadOnlyList<VisitorTourCard> Tours => _tours;

    public VisitorLanguageOption CurrentLanguage => _languages.First(language => language.Code == SelectedLanguageCode);

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
        _filteredPoisCache ??= _pois
            .Where(poi => SelectedCategoryId == "all" || poi.CategoryId == SelectedCategoryId)
            .Where(MatchesSearch)
            .ToArray();

    public IReadOnlyList<VisitorPoi> FeaturedPois =>
        _featuredPoisCache ??= FilteredPois
            .OrderBy(poi => poi.DistanceMeters)
            .Take(5)
            .ToArray();

    public IReadOnlyList<VisitorPoi> DiscoverPois =>
        _discoverPoisCache ??= FilteredPois
            .Where(HasReadyAudioForSelectedLanguage)
            .ToArray();

    public IReadOnlyList<VisitorPoi> FeaturedDiscoverPois =>
        _featuredDiscoverPoisCache ??= DiscoverPois
            .OrderBy(poi => poi.DistanceMeters)
            .Take(5)
            .ToArray();

    public static VisitorShellState CreateDefault()
    {
        var state = CreateBaseState();
        state.ApplyContent(VisitorContentSnapshot.CreateDemo());
        state.SeedSettingsDemoData();
        return state;
    }

    public static VisitorShellState CreateRuntimeDefault()
    {
        var state = CreateBaseState();
        state.DataSourceLabel = "Đang chờ đồng bộ";
        state.SyncMessage = "Đang chờ đồng bộ dữ liệu từ máy chủ.";
        state.IsUsingFallbackData = true;
        return state;
    }

    private static VisitorShellState CreateBaseState()
    {
        return new VisitorShellState(
            languages:
            [
                new VisitorLanguageOption("vi", "Tiếng Việt", "Mặc định", "VN"),
                new VisitorLanguageOption("en", "English", "Tiếng Anh", "GB"),
                new VisitorLanguageOption("ja", "日本語", "Tiếng Nhật", "JP"),
                new VisitorLanguageOption("ko", "한국어", "Tiếng Hàn", "KR"),
                new VisitorLanguageOption("zh", "中文", "Tiếng Trung", "CN"),
                new VisitorLanguageOption("fr", "Français", "Tiếng Pháp", "FR")
            ],
            categories:
            [
                new VisitorCategory("all", "Tất cả", "🏷️", "is-history")
            ],
            pois: [],
            notifications:
            [
                new VisitorNotification("Đang ở gần Cầu Khánh Hội", "Bật audio tự động để nghe khi tới geofence.", "2 phút trước"),
                new VisitorNotification("Tour mới vừa mở", "Khám phá tuyến Ven sông Khánh Hội có 4 điểm dừng.", "10 phút trước"),
                new VisitorNotification("Ngôn ngữ English sẵn sàng", "Bạn có thể đổi ngôn ngữ ở header bất kỳ lúc nào.", "Hôm nay")
            ],
            tours: []);
    }

    public void ApplyContent(VisitorContentSnapshot snapshot, bool isFallback = true, string? sourceLabel = null, string? syncMessage = null)
    {
        _categories.Clear();
        _categories.AddRange(BuildCategoryFilters(snapshot));

        _pois.Clear();
        _pois.AddRange(snapshot.Pois);
        InvalidatePoiViews();

        _tours.Clear();
        _tours.AddRange(snapshot.Tours);

        IsUsingFallbackData = isFallback;
        DataSourceLabel = sourceLabel ?? (isFallback ? "Demo fallback" : "Live API");
        SyncMessage = syncMessage ?? (isFallback ? "Đang dùng dữ liệu demo cục bộ." : "Đã đồng bộ dữ liệu từ máy chủ.");

        EnsureSelectedCategoryStillVisible();
        EnsureSelectedPoiStillVisible();
        EnsureSelectedTourStillVisible();
        EnsureActiveTourStillVisible();
    }

    public void UpdateLocation(VisitorLocationSnapshot location)
    {
        CurrentLocation = location;
        LocationPermissionGranted = location.PermissionGranted;
        LocationStatusLabel = VisitorLocationStatusFormatter.Build(location);

        var projectedPois = VisitorPoiDistanceProjector.Apply(_pois, location);
        if (!ReferenceEquals(projectedPois, _pois))
        {
            _pois.Clear();
            _pois.AddRange(projectedPois);
            InvalidatePoiViews();
        }
    }

    public void ApplyProximityFocus(VisitorProximityMatch? proximity)
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

    public void SetAudioCue(VisitorAudioCue cue)
    {
        CurrentAudioCue = cue;
        AudioStatusLabel = cue.StatusLabel;
        AudioElapsedSeconds = 0;
        AudioDurationSeconds = cue.DurationSeconds;
        AudioPlaybackState = cue.IsAvailable ? VisitorAudioPlaybackState.Ready : VisitorAudioPlaybackState.Error;
    }

    public void SetAudioPlaybackState(VisitorAudioPlaybackState playbackState, string? statusLabel = null)
    {
        AudioPlaybackState = playbackState;

        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            AudioStatusLabel = statusLabel;
        }

        if (playbackState == VisitorAudioPlaybackState.Playing)
        {
            UpsertCurrentListeningHistoryEntry();
        }
    }

    public void UpdateAudioProgress(int elapsedSeconds, int? durationSeconds = null)
    {
        AudioDurationSeconds = durationSeconds is > 0 ? durationSeconds.Value : AudioDurationSeconds;
        AudioElapsedSeconds = Math.Clamp(elapsedSeconds, 0, Math.Max(AudioDurationSeconds, elapsedSeconds));
        UpdateCurrentListeningHistoryProgress();
    }

    public void ContinueFromWelcome()
    {
        CurrentStep = VisitorIntroStep.Language;
    }

    public void SelectLanguage(string languageCode)
    {
        if (!_languages.Any(language => language.Code == languageCode))
        {
            return;
        }

        SelectedLanguageCode = languageCode;
        InvalidatePoiViews();
    }

    public void AdvanceFromLanguageSelection()
    {
        CurrentStep = VisitorIntroStep.Permissions;
    }

    public void ChangeLanguage(string languageCode)
    {
        if (_languages.Any(language => language.Code == languageCode))
        {
            SelectedLanguageCode = languageCode;
            InvalidatePoiViews();
        }
    }

    public void CompletePermissions(bool granted)
    {
        LocationPermissionGranted = granted;
        CurrentStep = VisitorIntroStep.Ready;

        if (SelectedPoi is null && _pois.Count > 0)
        {
            OpenPoi(_pois[0].Id);
        }
    }

    public void EnterReadyFromExternalEntry()
    {
        CurrentStep = VisitorIntroStep.Ready;
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

    public void ApplyQrNavigationTarget(VisitorQrNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        EnterReadyFromExternalEntry();

        switch (target.Kind)
        {
            case VisitorQrTargetKind.Poi when !string.IsNullOrWhiteSpace(target.TargetId) && _pois.Any(poi => poi.Id == target.TargetId):
                OpenPoi(target.TargetId);
                return;

            default:
                SwitchTab(VisitorTab.Map);
                return;
        }
    }

    public void SwitchTab(VisitorTab tab)
    {
        CurrentTab = tab;

        if (tab != VisitorTab.Settings)
        {
            CurrentSettingsScreen = VisitorSettingsScreen.Overview;
        }

        if (tab == VisitorTab.Map && SelectedPoi is null && _pois.Count > 0)
        {
            OpenPoi(_pois[0].Id);
        }

        if (tab == VisitorTab.Tours && SelectedTour is null && _tours.Count > 0)
        {
            SelectedTourId = _tours[0].Id;
        }
    }

    public void OpenSettingsScreen(VisitorSettingsScreen screen)
    {
        CurrentTab = VisitorTab.Settings;
        CurrentSettingsScreen = screen;
    }

    public void CloseSettingsScreen()
    {
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void SelectCategory(string categoryId)
    {
        if (!_categories.Any(category => category.Id == categoryId))
        {
            return;
        }

        if (SelectedCategoryId == categoryId)
        {
            return;
        }

        SelectedCategoryId = categoryId;
        InvalidatePoiViews();
        EnsureSelectedPoiStillVisible();
    }

    public void SetSearchTerm(string searchTerm)
    {
        var trimmedSearchTerm = searchTerm.Trim();
        if (SearchTerm == trimmedSearchTerm)
        {
            return;
        }

        SearchTerm = trimmedSearchTerm;
        InvalidatePoiViews();
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

    public void SetAudioSourcePreference(VisitorAudioSourcePreference preference)
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

    public void ApplyBackgroundTrackingStatus(VisitorBackgroundTrackingStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        GpsPreferences = GpsPreferences with
        {
            StatusLabel = status.StatusLabel
        };
    }

    public void SetGpsAutoFocusEnabled(bool isEnabled)
    {
        GpsPreferences = GpsPreferences with { AutoFocusEnabled = isEnabled };
    }

    public void SetGpsAccuracyMode(VisitorGpsAccuracyMode mode)
    {
        var batteryLabel = mode switch
        {
            VisitorGpsAccuracyMode.High => "High accuracy • pin giảm nhanh hơn",
            VisitorGpsAccuracyMode.BatterySaver => "Battery saver • giảm tần suất GPS",
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
        CurrentTab = VisitorTab.Map;
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
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
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void SelectTour(string tourId)
    {
        if (_tours.Any(tour => tour.Id == tourId))
        {
            SelectedTourId = tourId;
            CurrentTab = VisitorTab.Tours;
            CurrentSettingsScreen = VisitorSettingsScreen.Overview;
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
        ActiveTourSession = new VisitorTourSession(
            TourId: tour.Id,
            TourTitle: tour.Title,
            CurrentStopSequence: 0,
            TotalStops: tour.StopPoiIds.Count,
            NextPoiId: nextPoiId,
            NextPoiName: ResolvePoiName(nextPoiId),
            IsCompleted: false);

        OpenPoi(nextPoiId);
    }

    public void ApplyServerTourSession(VisitorTourSession session)
    {
        ActiveTourSession = session;
        SelectedTourId = session.TourId;

        if (!string.IsNullOrWhiteSpace(session.NextPoiId))
        {
            OpenPoi(session.NextPoiId);
            return;
        }

        CurrentTab = VisitorTab.Tours;
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
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
            new VisitorCachedAudioItem("cache-poi-khanh-hoi-vi", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "vi", "Recorded", 6.4, "Cập nhật 12 phút trước"),
            new VisitorCachedAudioItem("cache-poi-khanh-hoi-en", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "en", "Google TTS", 4.1, "Cập nhật 12 phút trước"),
            new VisitorCachedAudioItem("cache-poi-ben-nha-rong-vi", "poi-ben-nha-rong", "Bến Nhà Rồng", "vi", "Recorded", 5.8, "Cập nhật 1 giờ trước"),
            new VisitorCachedAudioItem("cache-poi-ben-nha-rong-ja", "poi-ben-nha-rong", "Bến Nhà Rồng", "ja", "Google TTS", 4.6, "Cập nhật 1 giờ trước"),
            new VisitorCachedAudioItem("cache-poi-cho-ben-thanh-zh", "poi-cho-ben-thanh", "Chợ Bến Thành", "zh", "Google TTS", 4.9, "Cập nhật hôm nay"),
            new VisitorCachedAudioItem("cache-poi-pho-dem-fr", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "fr", "Google TTS", 3.9, "Cập nhật hôm nay")
        ]);

        _listeningHistoryDays.Clear();
        _listeningHistoryDays.AddRange(
        [
            new VisitorListeningHistoryDay(
                "Hôm nay",
                [
                    new VisitorListeningHistoryEntry("history-1", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "Ven sông", "vi", "08:42", "3:12", 100),
                    new VisitorListeningHistoryEntry("history-2", "poi-ben-nha-rong", "Bến Nhà Rồng", "Lịch sử", "en", "09:15", "2:40", 76),
                    new VisitorListeningHistoryEntry("history-3", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "Đêm", "fr", "11:30", "2:18", 42)
                ]),
            new VisitorListeningHistoryDay(
                "Hôm qua",
                [
                    new VisitorListeningHistoryEntry("history-4", "poi-cho-ben-thanh", "Chợ Bến Thành", "Ẩm thực", "zh", "17:22", "3:05", 100),
                    new VisitorListeningHistoryEntry("history-5", "poi-tiem-banh-mi-co-lan", "Tiệm Bánh Mì Cô Lan", "Bánh mì", "vi", "18:04", "2:12", 63)
                ])
        ]);
    }

    private void UpsertCurrentListeningHistoryEntry()
    {
        if (CurrentAudioCue is not { IsAvailable: true } cue || SelectedPoi is null)
        {
            return;
        }

        if (!string.Equals(cue.PoiId, SelectedPoi.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var todayLabel = "Hôm nay";
        var todayIndex = _listeningHistoryDays.FindIndex(day =>
            string.Equals(day.Label, todayLabel, StringComparison.OrdinalIgnoreCase));
        var entries = todayIndex >= 0
            ? _listeningHistoryDays[todayIndex].Entries.ToList()
            : [];
        var existingIndex = entries.FindIndex(entry =>
            string.Equals(entry.PoiId, cue.PoiId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.LanguageCode, cue.LanguageCode, StringComparison.OrdinalIgnoreCase));
        var existingEntry = existingIndex >= 0 ? entries[existingIndex] : null;

        if (existingIndex >= 0)
        {
            entries.RemoveAt(existingIndex);
        }

        entries.Insert(0, new VisitorListeningHistoryEntry(
            existingEntry?.Id ?? $"history-{Guid.NewGuid():N}",
            cue.PoiId,
            SelectedPoi.Name,
            SelectedPoi.CategoryLabel,
            cue.LanguageCode,
            DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture),
            FormatDuration(cue.DurationSeconds),
            Math.Max(existingEntry?.CompletionPercent ?? 0, CalculateAudioCompletionPercent())));

        var today = new VisitorListeningHistoryDay(todayLabel, entries);
        if (todayIndex >= 0)
        {
            _listeningHistoryDays[todayIndex] = today;
            return;
        }

        _listeningHistoryDays.Insert(0, today);
    }

    private void UpdateCurrentListeningHistoryProgress()
    {
        if (CurrentAudioCue is not { IsAvailable: true } cue)
        {
            return;
        }

        for (var dayIndex = 0; dayIndex < _listeningHistoryDays.Count; dayIndex++)
        {
            var day = _listeningHistoryDays[dayIndex];
            var entries = day.Entries.ToList();
            var entryIndex = entries.FindIndex(entry =>
                string.Equals(entry.PoiId, cue.PoiId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.LanguageCode, cue.LanguageCode, StringComparison.OrdinalIgnoreCase));

            if (entryIndex < 0)
            {
                continue;
            }

            var entry = entries[entryIndex];
            entries[entryIndex] = entry with
            {
                DurationLabel = FormatDuration(AudioDurationSeconds),
                CompletionPercent = Math.Max(entry.CompletionPercent, CalculateAudioCompletionPercent())
            };
            _listeningHistoryDays[dayIndex] = day with { Entries = entries };
            return;
        }
    }

    private int CalculateAudioCompletionPercent()
    {
        if (AudioDurationSeconds <= 0)
        {
            return 0;
        }

        return (int)Math.Clamp(
            Math.Round(AudioElapsedSeconds * 100d / AudioDurationSeconds, MidpointRounding.AwayFromZero),
            0d,
            100d);
    }

    private void ResetDiscoverFilters()
    {
        if (SelectedCategoryId == "all" && SearchTerm == string.Empty)
        {
            return;
        }

        SelectedCategoryId = "all";
        SearchTerm = string.Empty;
        InvalidatePoiViews();
    }

    private void InvalidatePoiViews()
    {
        _filteredPoisCache = null;
        _featuredPoisCache = null;
        _discoverPoisCache = null;
        _featuredDiscoverPoisCache = null;
    }

    private bool HasReadyAudioForSelectedLanguage(VisitorPoi poi)
    {
        return poi.ReadyAudioLanguageCodes.Any(languageCode =>
            string.Equals(languageCode, SelectedLanguageCode, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureSelectedCategoryStillVisible()
    {
        if (_categories.Any(category => string.Equals(category.Id, SelectedCategoryId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedCategoryId = "all";
    }

    private static IReadOnlyList<VisitorCategory> BuildCategoryFilters(VisitorContentSnapshot snapshot)
    {
        var categories = new List<VisitorCategory>
        {
            new("all", "Tất cả", "🏷️", "is-history")
        };

        var liveCategories = (snapshot.Categories ?? [])
            .Where(category => !string.IsNullOrWhiteSpace(category.Id) && !string.Equals(category.Id, "all", StringComparison.OrdinalIgnoreCase))
            .GroupBy(category => category.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        if (liveCategories.Length > 0)
        {
            categories.AddRange(liveCategories);
            return categories;
        }

        categories.AddRange(
            snapshot.Pois
                .Where(poi => !string.IsNullOrWhiteSpace(poi.CategoryId))
                .GroupBy(poi => poi.CategoryId, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var firstPoi = group.First();
                    return new VisitorCategory(
                        firstPoi.CategoryId,
                        string.IsNullOrWhiteSpace(firstPoi.CategoryLabel) ? firstPoi.District : firstPoi.CategoryLabel,
                        VisitorCategoryPresentationFormatter.GetPoiIcon(firstPoi, []),
                        VisitorCategoryPresentationFormatter.GetCategoryTone(firstPoi.CategoryId, [], firstPoi.CategoryLabel));
                })
                .OrderBy(category => category.Label, StringComparer.CurrentCultureIgnoreCase));

        return categories;
    }

    private string ResolvePoiName(string poiId)
    {
        return _pois.FirstOrDefault(poi => poi.Id == poiId)?.Name ?? "POI kế tiếp";
    }

    private bool MatchesSearch(VisitorPoi poi)
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

public sealed record VisitorLanguageOption(string Code, string Label, string SubLabel, string ChipLabel);

public sealed record VisitorCategory(string Id, string Label, string MarkerLabel, string ToneKey = "is-history");

public sealed record VisitorPoi(
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
    int GeofenceRadiusMeters = 30,
    string? ImageUrl = null,
    IReadOnlyList<string>? ReadyAudioLanguageCodesRaw = null)
{
    public IReadOnlyList<string> ReadyAudioLanguageCodes { get; init; } = ReadyAudioLanguageCodesRaw ?? Array.Empty<string>();
}

public sealed record VisitorNotification(string Title, string Body, string TimeLabel);

public sealed record VisitorTourCard(
    string Id,
    string Title,
    string StopCountLabel,
    string DurationLabel,
    string DifficultyLabel,
    string Description,
    IReadOnlyList<string> StopPoiIds);

public sealed record VisitorTourSession(
    string TourId,
    string TourTitle,
    int CurrentStopSequence,
    int TotalStops,
    string? NextPoiId,
    string NextPoiName,
    bool IsCompleted,
    bool IsServerBacked = false,
    TourSessionStatus? SyncStatus = null);
