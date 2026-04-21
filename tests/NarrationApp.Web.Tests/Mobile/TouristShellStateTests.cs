using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristShellStateTests
{
    [Fact]
    public void CreateDefault_StartsInWelcomeStep()
    {
        var state = TouristShellState.CreateDefault();

        Assert.Equal(TouristIntroStep.Welcome, state.CurrentStep);
        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.Equal(TouristSettingsScreen.Overview, state.CurrentSettingsScreen);
        Assert.True(state.IsGuestMode);
        Assert.NotEmpty(state.Pois);
    }

    [Fact]
    public void CreateDefault_UsesExpandedLanguageSetForTouristAudio()
    {
        var state = TouristShellState.CreateDefault();

        Assert.Equal(["vi", "en", "ja", "ko", "zh", "fr"], state.Languages.Select(language => language.Code));
    }

    [Fact]
    public void OnboardingFlow_MovesIntoReadyState()
    {
        var state = TouristShellState.CreateDefault();

        state.ContinueFromWelcome();
        state.SelectLanguage("en");
        state.CompletePermissions(granted: true);

        Assert.Equal(TouristIntroStep.Ready, state.CurrentStep);
        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.Equal("en", state.SelectedLanguageCode);
        Assert.True(state.LocationPermissionGranted);
    }

    [Fact]
    public void ContinueFromWelcome_MovesIntoLanguageStep()
    {
        var state = TouristShellState.CreateDefault();

        state.ContinueFromWelcome();

        Assert.Equal(TouristIntroStep.Language, state.CurrentStep);
    }

    [Fact]
    public void FilteredPois_RespondToCategoryAndSearch()
    {
        var state = TouristShellState.CreateDefault();

        state.SelectCategory("food");

        Assert.All(state.FilteredPois, poi => Assert.Equal("food", poi.CategoryId));

        state.SetSearchTerm("banh");

        var matchingPoi = Assert.Single(state.FilteredPois);
        Assert.Equal("Tiệm Bánh Mì Cô Lan", matchingPoi.Name);
    }

    [Fact]
    public void OpeningPoi_SelectsPoiAndKeepsPlayerVisible()
    {
        var state = TouristShellState.CreateDefault();

        state.OpenPoi("poi-khanh-hoi-bridge");

        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.NotNull(state.SelectedPoi);
        Assert.Equal("poi-khanh-hoi-bridge", state.SelectedPoi!.Id);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void PreviewPoi_SelectsPoiWithoutForcingMapSheet()
    {
        var state = TouristShellState.CreateDefault();
        state.SwitchTab(TouristTab.Discover);

        state.PreviewPoi("poi-khanh-hoi-bridge");

        Assert.Equal(TouristTab.Discover, state.CurrentTab);
        Assert.Equal("poi-khanh-hoi-bridge", state.SelectedPoiId);
        Assert.False(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void SettingsNavigation_OpensAndClosesSubScreens()
    {
        var state = TouristShellState.CreateDefault();
        state.SwitchTab(TouristTab.Settings);

        state.OpenSettingsScreen(TouristSettingsScreen.Audio);
        Assert.Equal(TouristSettingsScreen.Audio, state.CurrentSettingsScreen);

        state.CloseSettingsScreen();
        Assert.Equal(TouristSettingsScreen.Overview, state.CurrentSettingsScreen);
    }

    [Fact]
    public void AudioPreferences_CanBeCustomizedForMobilePlayback()
    {
        var state = TouristShellState.CreateDefault();

        state.SetAudioAutoPlayEnabled(false);
        state.SetAudioSpokenAnnouncementsEnabled(false);
        state.SetAudioAutoAdvanceEnabled(false);
        state.SetAudioSourcePreference(TouristAudioSourcePreference.TextToSpeech);
        state.SetAudioPlaybackSpeed(2d);

        Assert.False(state.AudioPreferences.AutoPlayEnabled);
        Assert.False(state.AudioPreferences.SpokenAnnouncementsEnabled);
        Assert.False(state.AudioPreferences.AutoAdvanceEnabled);
        Assert.Equal(TouristAudioSourcePreference.TextToSpeech, state.AudioPreferences.SourcePreference);
        Assert.Equal(2d, state.AudioPreferences.DefaultPlaybackSpeed);
    }

    [Fact]
    public void CacheManager_CanRemoveSingleItemAndClearAll()
    {
        var state = TouristShellState.CreateDefault();
        var firstItemId = state.CachedAudioItems[0].Id;

        state.RemoveCachedAudioItem(firstItemId);

        Assert.DoesNotContain(state.CachedAudioItems, item => item.Id == firstItemId);

        state.ClearCachedAudioItems();

        Assert.Empty(state.CachedAudioItems);
    }

    [Fact]
    public void ApplyContent_ReplacesPoisAndToursAndKeepsStateReady()
    {
        var state = TouristShellState.CreateDefault();
        var snapshot = new TouristContentSnapshot(
            [
                new TouristPoi(
                    "poi-live-001",
                    "Bến Nhà Rồng",
                    "river",
                    "Di tích",
                    "Di tích",
                    "Live API",
                    "Điểm đến lấy từ API thật.",
                    "Ven sông",
                    32,
                    48,
                    140,
                    "2:40",
                    "Từ máy chủ",
                    10.7680,
                    106.7068)
            ],
            [
                new TouristTourCard(
                    "tour-live-001",
                    "Tour ven sông",
                    "3 điểm dừng",
                    "30 phút",
                    "Dễ đi bộ",
                    "Tour lấy từ API",
                    ["poi-live-001"])
            ]);

        state.ApplyContent(snapshot);

        Assert.Single(state.Pois);
        Assert.Single(state.Tours);
        Assert.Equal("poi-live-001", state.SelectedPoiId);
        Assert.Equal("tour-live-001", state.SelectedTourId);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void ApplyProximityFocus_SelectsPoiAndShowsAutoNarrationPrompt()
    {
        var state = TouristShellState.CreateDefault();

        state.ApplyProximityFocus(new TouristProximityMatch("poi-ben-nha-rong", "Bến Nhà Rồng", 42, 120));

        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.Equal("poi-ben-nha-rong", state.SelectedPoiId);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
        Assert.True(state.HasAutoNarrationPrompt);
        Assert.Contains("42m", state.AutoNarrationPrompt);
    }

    [Fact]
    public void SetAudioCue_StoresPlayableAudioForMiniPlayer()
    {
        var state = TouristShellState.CreateDefault();
        var cue = new TouristAudioCue(
            PoiId: "poi-ben-nha-rong",
            LanguageCode: "vi",
            StreamUrl: "https://10.0.2.2:5001/api/audio/20/stream",
            DurationSeconds: 125,
            IsAvailable: true,
            StatusLabel: "Sẵn sàng phát",
            IsPreferredLanguage: true);

        state.SetAudioCue(cue);

        Assert.NotNull(state.CurrentAudioCue);
        Assert.True(state.CanPlayAudio);
        Assert.Equal("Sẵn sàng phát", state.AudioStatusLabel);
        Assert.Equal("0:00", state.AudioElapsedLabel);
        Assert.Equal("2:05", state.AudioDurationLabel);
    }

    [Fact]
    public void SetAudioPlaybackState_TracksPlayerLifecycle()
    {
        var state = TouristShellState.CreateDefault();

        state.SetAudioPlaybackState(TouristAudioPlaybackState.Playing, "Đang phát tự động");
        Assert.True(state.IsAudioPlaying);
        Assert.Equal("Đang phát tự động", state.AudioStatusLabel);

        state.SetAudioPlaybackState(TouristAudioPlaybackState.Paused, "Đã tạm dừng");
        Assert.False(state.IsAudioPlaying);
        Assert.Equal("Đã tạm dừng", state.AudioStatusLabel);
    }

    [Fact]
    public void UpdateAudioProgress_TracksElapsedTimeAndPercent()
    {
        var state = TouristShellState.CreateDefault();
        state.SetAudioCue(new TouristAudioCue(
            PoiId: "poi-ben-nha-rong",
            LanguageCode: "vi",
            StreamUrl: "https://10.0.2.2:5001/api/audio/20/stream",
            DurationSeconds: 125,
            IsAvailable: true,
            StatusLabel: "Sẵn sàng phát",
            IsPreferredLanguage: true));

        state.UpdateAudioProgress(50, 125);

        Assert.Equal(50, state.AudioElapsedSeconds);
        Assert.Equal(125, state.AudioDurationSeconds);
        Assert.Equal("0:50", state.AudioElapsedLabel);
        Assert.Equal("2:05", state.AudioDurationLabel);
        Assert.Equal(40d, state.AudioProgressPercent, 1);
    }

    [Fact]
    public void SetAudioCue_ResetsPreviousProgress()
    {
        var state = TouristShellState.CreateDefault();
        state.SetAudioCue(new TouristAudioCue(
            PoiId: "poi-ben-nha-rong",
            LanguageCode: "vi",
            StreamUrl: "https://10.0.2.2:5001/api/audio/20/stream",
            DurationSeconds: 125,
            IsAvailable: true,
            StatusLabel: "Sẵn sàng phát",
            IsPreferredLanguage: true));
        state.UpdateAudioProgress(72, 125);

        state.SetAudioCue(new TouristAudioCue(
            PoiId: "poi-7",
            LanguageCode: "en",
            StreamUrl: "https://10.0.2.2:5001/api/audio/12/stream",
            DurationSeconds: 88,
            IsAvailable: true,
            StatusLabel: "Sẵn sàng phát • EN • TTS",
            IsPreferredLanguage: true));

        Assert.Equal(0, state.AudioElapsedSeconds);
        Assert.Equal(88, state.AudioDurationSeconds);
        Assert.Equal(0d, state.AudioProgressPercent);
        Assert.Equal("0:00", state.AudioElapsedLabel);
        Assert.Equal("1:28", state.AudioDurationLabel);
    }

    [Fact]
    public void StartTour_CreatesActiveSessionAndOpensFirstStop()
    {
        var state = TouristShellState.CreateDefault();

        state.StartTour("tour-river");

        Assert.Equal("tour-river", state.SelectedTourId);
        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.Equal("poi-khanh-hoi-bridge", state.SelectedPoiId);
        Assert.NotNull(state.ActiveTourSession);
        Assert.Equal("tour-river", state.ActiveTourSession!.TourId);
        Assert.Equal(0, state.ActiveTourSession.CurrentStopSequence);
        Assert.Equal(4, state.ActiveTourSession.TotalStops);
        Assert.Equal("poi-khanh-hoi-bridge", state.ActiveTourSession.NextPoiId);
        Assert.Equal("Cầu Khánh Hội", state.ActiveTourSession.NextPoiName);
    }

    [Fact]
    public void AdvanceActiveTour_OnlyMovesWhenExpectedPoiMatches()
    {
        var state = TouristShellState.CreateDefault();
        state.StartTour("tour-river");

        var wrongPoiAccepted = state.AdvanceActiveTour("poi-ben-nha-rong");
        Assert.False(wrongPoiAccepted);
        Assert.Equal(0, state.ActiveTourSession!.CurrentStopSequence);
        Assert.Equal("poi-khanh-hoi-bridge", state.ActiveTourSession.NextPoiId);

        var firstPoiAccepted = state.AdvanceActiveTour("poi-khanh-hoi-bridge");
        Assert.True(firstPoiAccepted);
        Assert.Equal(1, state.ActiveTourSession.CurrentStopSequence);
        Assert.Equal("poi-ben-nha-rong", state.ActiveTourSession.NextPoiId);
        Assert.Equal("Bến Nhà Rồng", state.ActiveTourSession.NextPoiName);
    }

    [Fact]
    public void ApplyQrNavigationTarget_Poi_SkipsOnboardingAndOpensRequestedPoi()
    {
        var state = TouristShellState.CreateDefault();
        state.ContinueFromWelcome();
        state.SelectLanguage("en");
        state.SelectCategory("food");
        state.SetSearchTerm("banh");
        state.ApplyContent(new TouristContentSnapshot(
            [
                new TouristPoi(
                    "poi-7",
                    "Cầu Khánh Hội",
                    "history",
                    "Di tích",
                    "Quận 4",
                    "Di sản ven sông",
                    "Điểm nối trung tâm với trục thương cảng xưa của Sài Gòn.",
                    "Bắt đầu tour ven sông",
                    18,
                    52,
                    180,
                    "3:12",
                    "Sẵn sàng",
                    10.7609,
                    106.7054)
            ],
            []));

        state.ApplyQrNavigationTarget(new TouristQrNavigationTarget("QR-001", TouristQrTargetKind.Poi, "poi-7"));

        Assert.Equal(TouristIntroStep.Ready, state.CurrentStep);
        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.Equal("all", state.SelectedCategoryId);
        Assert.Equal(string.Empty, state.SearchTerm);
        Assert.Equal("poi-7", state.SelectedPoiId);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void ApplyQrNavigationTarget_Tour_SkipsOnboardingAndSelectsRequestedTour()
    {
        var state = TouristShellState.CreateDefault();
        state.ApplyContent(new TouristContentSnapshot(
            [
                new TouristPoi(
                    "poi-7",
                    "Cầu Khánh Hội",
                    "history",
                    "Di tích",
                    "Quận 4",
                    "Di sản ven sông",
                    "Điểm nối trung tâm với trục thương cảng xưa của Sài Gòn.",
                    "Bắt đầu tour ven sông",
                    18,
                    52,
                    180,
                    "3:12",
                    "Sẵn sàng",
                    10.7609,
                    106.7054)
            ],
            [
                new TouristTourCard(
                    "tour-2",
                    "Ven sông",
                    "1 điểm dừng",
                    "12 phút",
                    "Nhanh",
                    "Tour ngắn.",
                    ["poi-7"])
            ]));

        state.ApplyQrNavigationTarget(new TouristQrNavigationTarget("QR-TOUR-2", TouristQrTargetKind.Tour, "tour-2"));

        Assert.Equal(TouristIntroStep.Ready, state.CurrentStep);
        Assert.Equal(TouristTab.Tours, state.CurrentTab);
        Assert.Equal("tour-2", state.SelectedTourId);
    }

    [Fact]
    public void ApplyQrNavigationTarget_OpenApp_SkipsOnboardingWithoutBreakingDiscoverState()
    {
        var state = TouristShellState.CreateDefault();

        state.ApplyQrNavigationTarget(new TouristQrNavigationTarget("QR-APP-1", TouristQrTargetKind.OpenApp, null));

        Assert.Equal(TouristIntroStep.Ready, state.CurrentStep);
        Assert.Equal(TouristTab.Map, state.CurrentTab);
        Assert.NotNull(state.SelectedPoi);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }
}
