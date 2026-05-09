namespace NarrationApp.Mobile.Features.Home;

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

    public string AudioElapsedLabel => VisitorDurationFormatter.FormatSeconds(AudioElapsedSeconds);

    public string AudioDurationLabel => VisitorDurationFormatter.FormatSeconds(AudioDurationSeconds);

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
        _filteredPoisCache ??= VisitorPoiFilter.Apply(_pois, SelectedCategoryId, SearchTerm);

    public IReadOnlyList<VisitorPoi> FeaturedPois =>
        _featuredPoisCache ??= FilteredPois
            .OrderBy(poi => poi.DistanceMeters)
            .Take(5)
            .ToArray();

    public IReadOnlyList<VisitorPoi> DiscoverPois =>
        _discoverPoisCache ??= VisitorPoiFilter.WithReadyAudioForLanguage(FilteredPois, SelectedLanguageCode);

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
            languages: VisitorLanguageCatalog.Defaults.ToList(),
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
        ApplyLanguages(snapshot.Languages);

        _categories.Clear();
        _categories.AddRange(VisitorCategoryFilterBuilder.Build(snapshot));

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
        ApplyPendingQrNavigationTargetIfReady();
    }

    private void ApplyLanguages(IReadOnlyList<VisitorLanguageOption>? languages)
    {
        if (languages is null || languages.Count == 0)
        {
            return;
        }

        var normalizedLanguages = languages
            .Where(language => !string.IsNullOrWhiteSpace(language.Code))
            .GroupBy(language => language.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(language => language with { Code = language.Code.Trim().ToLowerInvariant() })
            .ToArray();

        if (normalizedLanguages.Length == 0)
        {
            return;
        }

        _languages.Clear();
        _languages.AddRange(normalizedLanguages);

        if (_languages.Any(language => string.Equals(language.Code, SelectedLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedLanguageCode = _languages.FirstOrDefault(language => string.Equals(language.Code, "vi", StringComparison.OrdinalIgnoreCase))?.Code
            ?? _languages[0].Code;
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
        var previousProximityPoiId = ActiveProximity?.PoiId;
        ActiveProximity = proximity;

        if (proximity is null)
        {
            AutoNarrationPrompt = "Chưa ở trong vùng phát tự động.";
            _dismissedProximityPoiId = null;
            return;
        }

        AutoNarrationPrompt = $"Bạn đang ở gần {proximity.PoiName} ({proximity.DistanceMeters}m). Sẵn sàng phát audio tự động.";

        var enteredDifferentPoi = !string.Equals(previousProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase);
        if (enteredDifferentPoi && !string.Equals(_dismissedProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = null;
        }

        if (string.Equals(_dismissedProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase))
        {
            PreviewPoi(proximity.PoiId);
            return;
        }

        OpenPoi(proximity.PoiId);

        if (enteredDifferentPoi)
        {
            AddProximityNotification(proximity);
        }
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
            VisitorListeningHistoryUpdater.UpsertCurrent(
                _listeningHistoryDays,
                CurrentAudioCue,
                SelectedPoi,
                AudioElapsedSeconds,
                AudioDurationSeconds);
        }
    }

    public void UpdateAudioProgress(int elapsedSeconds, int? durationSeconds = null)
    {
        AudioDurationSeconds = durationSeconds is > 0 ? durationSeconds.Value : AudioDurationSeconds;
        AudioElapsedSeconds = Math.Clamp(elapsedSeconds, 0, Math.Max(AudioDurationSeconds, elapsedSeconds));
        VisitorListeningHistoryUpdater.UpdateProgress(
            _listeningHistoryDays,
            CurrentAudioCue,
            AudioElapsedSeconds,
            AudioDurationSeconds);
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
        }
    }

    public void ApplyQrNavigationTarget(VisitorQrNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        EnterReadyFromExternalEntry();

        _pendingQrNavigationTarget = null;
        if (TryApplyQrNavigationTarget(target))
        {
            return;
        }

        if (target.Kind is VisitorQrTargetKind.Poi or VisitorQrTargetKind.Tour && !string.IsNullOrWhiteSpace(target.TargetId))
        {
            _pendingQrNavigationTarget = target;
        }

        SwitchTab(VisitorTab.Map);
    }

    public void SwitchTab(VisitorTab tab)
    {
        CurrentTab = tab;

        if (tab != VisitorTab.Settings)
        {
            CurrentSettingsScreen = VisitorSettingsScreen.Overview;
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

    public void SetCachedAudioItems(IReadOnlyList<VisitorCachedAudioItem> items)
    {
        _cachedAudioItems.Clear();
        _cachedAudioItems.AddRange(items);
    }

    public void OpenPoi(string poiId)
    {
        if (!_pois.Any(poi => poi.Id == poiId))
        {
            return;
        }

        if (string.Equals(_dismissedProximityPoiId, poiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = null;
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
        if (ActiveProximity is not null
            && string.Equals(ActiveProximity.PoiId, SelectedPoiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = ActiveProximity.PoiId;
        }

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

        SelectedPoiId = null;
        ShowPoiSheet = false;
        ShowMiniPlayer = false;
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

    private void AddProximityNotification(VisitorProximityMatch proximity)
    {
        var title = $"Đang ở gần {proximity.PoiName}";
        var body = $"Đã vào bán kính {proximity.TriggerRadiusMeters}m, còn khoảng {proximity.DistanceMeters}m. Audio sẽ tự phát nếu đã sẵn sàng.";

        if (_notifications.FirstOrDefault() is { } latest
            && string.Equals(latest.Title, title, StringComparison.OrdinalIgnoreCase)
            && string.Equals(latest.Body, body, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _notifications.Insert(0, new VisitorNotification(title, body, "Vừa xong"));

        const int maxNotifications = 12;
        if (_notifications.Count > maxNotifications)
        {
            _notifications.RemoveRange(maxNotifications, _notifications.Count - maxNotifications);
        }
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

    private void ApplyPendingQrNavigationTargetIfReady()
    {
        if (_pendingQrNavigationTarget is null)
        {
            return;
        }

        if (TryApplyQrNavigationTarget(_pendingQrNavigationTarget))
        {
            _pendingQrNavigationTarget = null;
        }
    }

    private bool TryApplyQrNavigationTarget(VisitorQrNavigationTarget target)
    {
        switch (target.Kind)
        {
            case VisitorQrTargetKind.OpenApp:
                SwitchTab(VisitorTab.Map);
                return true;

            case VisitorQrTargetKind.Poi when !string.IsNullOrWhiteSpace(target.TargetId) && _pois.Any(poi => poi.Id == target.TargetId):
                OpenPoi(target.TargetId);
                return true;

            case VisitorQrTargetKind.Tour when IsTourReadyForQrStart(target.TargetId):
                StartTour(target.TargetId!);
                return true;

            default:
                return false;
        }
    }

    private bool IsTourReadyForQrStart(string? tourId)
    {
        if (string.IsNullOrWhiteSpace(tourId))
        {
            return false;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == tourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            return false;
        }

        var firstStopPoiId = tour.StopPoiIds[0];
        return _pois.Any(poi => poi.Id == firstStopPoiId);
    }

    private void SeedSettingsDemoData()
    {
        if (_pois.Count == 0)
        {
            return;
        }

        _cachedAudioItems.Clear();
        _cachedAudioItems.AddRange(VisitorSettingsDemoData.CreateCachedAudioItems());

        _listeningHistoryDays.Clear();
        _listeningHistoryDays.AddRange(VisitorSettingsDemoData.CreateListeningHistoryDays());
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

    private void EnsureSelectedCategoryStillVisible()
    {
        if (_categories.Any(category => string.Equals(category.Id, SelectedCategoryId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedCategoryId = "all";
    }

    private string ResolvePoiName(string poiId)
    {
        return _pois.FirstOrDefault(poi => poi.Id == poiId)?.Name ?? "POI kế tiếp";
    }
}
