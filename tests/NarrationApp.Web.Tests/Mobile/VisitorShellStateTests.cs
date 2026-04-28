using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorShellStateTests
{
    [Fact]
    public void CreateDefault_StartsInLanguageStep()
    {
        var state = VisitorShellState.CreateDefault();

        Assert.Equal(VisitorIntroStep.Language, state.CurrentStep);
        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.Equal(VisitorSettingsScreen.Overview, state.CurrentSettingsScreen);
        Assert.NotEmpty(state.Pois);
    }

    [Fact]
    public void CreateDefault_UsesExpandedLanguageSetForVisitorAudio()
    {
        var state = VisitorShellState.CreateDefault();

        Assert.Equal(["vi", "en", "ja", "ko", "zh", "fr"], state.Languages.Select(language => language.Code));
    }

    [Fact]
    public void OnboardingFlow_MovesIntoReadyState()
    {
        var state = VisitorShellState.CreateDefault();

        state.SelectLanguage("en");
        state.CompletePermissions(granted: true);

        Assert.Equal(VisitorIntroStep.Ready, state.CurrentStep);
        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.Equal("en", state.SelectedLanguageCode);
        Assert.True(state.LocationPermissionGranted);
    }

    [Fact]
    public void SelectingLanguage_MovesIntoPermissionsStep()
    {
        var state = VisitorShellState.CreateDefault();

        state.SelectLanguage("en");

        Assert.Equal(VisitorIntroStep.Permissions, state.CurrentStep);
    }

    [Fact]
    public void FilteredPois_RespondToCategoryAndSearch()
    {
        var state = VisitorShellState.CreateDefault();

        state.SelectCategory("food");

        Assert.All(state.FilteredPois, poi => Assert.Equal("food", poi.CategoryId));

        state.SetSearchTerm("banh");

        var matchingPoi = Assert.Single(state.FilteredPois);
        Assert.Equal("Tiệm Bánh Mì Cô Lan", matchingPoi.Name);
    }

    [Fact]
    public void Poi_view_lists_are_cached_until_filters_or_content_change()
    {
        var state = VisitorShellState.CreateDefault();

        var firstFiltered = state.FilteredPois;
        var secondFiltered = state.FilteredPois;
        var firstFeatured = state.FeaturedPois;
        var secondFeatured = state.FeaturedPois;

        Assert.Same(firstFiltered, secondFiltered);
        Assert.Same(firstFeatured, secondFeatured);

        state.SetSearchTerm("banh");

        Assert.NotSame(firstFiltered, state.FilteredPois);
        Assert.NotSame(firstFeatured, state.FeaturedPois);

        var filteredAfterSearch = state.FilteredPois;
        state.ApplyContent(VisitorContentSnapshot.CreateDemo());

        Assert.NotSame(filteredAfterSearch, state.FilteredPois);
    }

    [Fact]
    public void OpeningPoi_SelectsPoiAndKeepsPlayerVisible()
    {
        var state = VisitorShellState.CreateDefault();

        state.OpenPoi("poi-khanh-hoi-bridge");

        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.NotNull(state.SelectedPoi);
        Assert.Equal("poi-khanh-hoi-bridge", state.SelectedPoi!.Id);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void PreviewPoi_SelectsPoiWithoutForcingMapSheet()
    {
        var state = VisitorShellState.CreateDefault();
        state.SwitchTab(VisitorTab.Discover);

        state.PreviewPoi("poi-khanh-hoi-bridge");

        Assert.Equal(VisitorTab.Discover, state.CurrentTab);
        Assert.Equal("poi-khanh-hoi-bridge", state.SelectedPoiId);
        Assert.False(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void SettingsNavigation_OpensAndClosesSubScreens()
    {
        var state = VisitorShellState.CreateDefault();
        state.SwitchTab(VisitorTab.Settings);

        state.OpenSettingsScreen(VisitorSettingsScreen.Audio);
        Assert.Equal(VisitorSettingsScreen.Audio, state.CurrentSettingsScreen);

        state.CloseSettingsScreen();
        Assert.Equal(VisitorSettingsScreen.Overview, state.CurrentSettingsScreen);
    }

    [Fact]
    public void AudioPreferences_CanBeCustomizedForMobilePlayback()
    {
        var state = VisitorShellState.CreateDefault();

        state.SetAudioAutoPlayEnabled(false);
        state.SetAudioSpokenAnnouncementsEnabled(false);
        state.SetAudioAutoAdvanceEnabled(false);
        state.SetAudioSourcePreference(VisitorAudioSourcePreference.TextToSpeech);
        state.SetAudioPlaybackSpeed(2d);

        Assert.False(state.AudioPreferences.AutoPlayEnabled);
        Assert.False(state.AudioPreferences.SpokenAnnouncementsEnabled);
        Assert.False(state.AudioPreferences.AutoAdvanceEnabled);
        Assert.Equal(VisitorAudioSourcePreference.TextToSpeech, state.AudioPreferences.SourcePreference);
        Assert.Equal(2d, state.AudioPreferences.DefaultPlaybackSpeed);
    }

    [Fact]
    public void CacheManager_CanRemoveSingleItemAndClearAll()
    {
        var state = VisitorShellState.CreateDefault();
        var firstItemId = state.CachedAudioItems[0].Id;

        state.RemoveCachedAudioItem(firstItemId);

        Assert.DoesNotContain(state.CachedAudioItems, item => item.Id == firstItemId);

        state.ClearCachedAudioItems();

        Assert.Empty(state.CachedAudioItems);
    }

    [Fact]
    public void ApplyContent_ReplacesPoisAndToursAndKeepsStateReady()
    {
        var state = VisitorShellState.CreateDefault();
        var snapshot = new VisitorContentSnapshot(
            [
                new VisitorPoi(
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
                new VisitorTourCard(
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
        var state = VisitorShellState.CreateDefault();

        state.ApplyProximityFocus(new VisitorProximityMatch("poi-ben-nha-rong", "Bến Nhà Rồng", 42, 120));

        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.Equal("poi-ben-nha-rong", state.SelectedPoiId);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
        Assert.True(state.HasAutoNarrationPrompt);
        Assert.Contains("42m", state.AutoNarrationPrompt);
    }

    [Fact]
    public void SetAudioCue_StoresPlayableAudioForMiniPlayer()
    {
        var state = VisitorShellState.CreateDefault();
        var cue = new VisitorAudioCue(
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
        var state = VisitorShellState.CreateDefault();

        state.SetAudioPlaybackState(VisitorAudioPlaybackState.Playing, "Đang phát tự động");
        Assert.True(state.IsAudioPlaying);
        Assert.Equal("Đang phát tự động", state.AudioStatusLabel);

        state.SetAudioPlaybackState(VisitorAudioPlaybackState.Paused, "Đã tạm dừng");
        Assert.False(state.IsAudioPlaying);
        Assert.Equal("Đã tạm dừng", state.AudioStatusLabel);
    }

    [Fact]
    public void UpdateAudioProgress_TracksElapsedTimeAndPercent()
    {
        var state = VisitorShellState.CreateDefault();
        state.SetAudioCue(new VisitorAudioCue(
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
        var state = VisitorShellState.CreateDefault();
        state.SetAudioCue(new VisitorAudioCue(
            PoiId: "poi-ben-nha-rong",
            LanguageCode: "vi",
            StreamUrl: "https://10.0.2.2:5001/api/audio/20/stream",
            DurationSeconds: 125,
            IsAvailable: true,
            StatusLabel: "Sẵn sàng phát",
            IsPreferredLanguage: true));
        state.UpdateAudioProgress(72, 125);

        state.SetAudioCue(new VisitorAudioCue(
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
        var state = VisitorShellState.CreateDefault();

        state.StartTour("tour-river");

        Assert.Equal("tour-river", state.SelectedTourId);
        Assert.Equal(VisitorTab.Map, state.CurrentTab);
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
        var state = VisitorShellState.CreateDefault();
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
        var state = VisitorShellState.CreateDefault();
        state.SelectLanguage("en");
        state.SelectCategory("food");
        state.SetSearchTerm("banh");
        state.ApplyContent(new VisitorContentSnapshot(
            [
                new VisitorPoi(
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

        state.ApplyQrNavigationTarget(new VisitorQrNavigationTarget("QR-001", VisitorQrTargetKind.Poi, "poi-7"));

        Assert.Equal(VisitorIntroStep.Ready, state.CurrentStep);
        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.Equal("all", state.SelectedCategoryId);
        Assert.Equal(string.Empty, state.SearchTerm);
        Assert.Equal("poi-7", state.SelectedPoiId);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }

    [Fact]
    public void ApplyQrNavigationTarget_OpenApp_SkipsOnboardingWithoutBreakingDiscoverState()
    {
        var state = VisitorShellState.CreateDefault();

        state.ApplyQrNavigationTarget(new VisitorQrNavigationTarget("QR-APP-1", VisitorQrTargetKind.OpenApp, null));

        Assert.Equal(VisitorIntroStep.Ready, state.CurrentStep);
        Assert.Equal(VisitorTab.Map, state.CurrentTab);
        Assert.NotNull(state.SelectedPoi);
        Assert.True(state.ShowPoiSheet);
        Assert.True(state.ShowMiniPlayer);
    }
}
