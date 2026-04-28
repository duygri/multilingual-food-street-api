using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class HomeMarkupTests
{
    [Fact]
    public void Mobile_home_keeps_markup_separate_from_behavior_code()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var codeBehindPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor.cs");
        var startupPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.Startup.razor.cs");
        var startupWorkPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.StartupWork.razor.cs");
        var mapRuntimePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.MapRuntime.razor.cs");
        var runtimePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.Runtime.razor.cs");
        var runtimeDisposalPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.RuntimeDisposal.razor.cs");
        var audioRuntimePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.AudioRuntime.razor.cs");
        var audioRuntimeStatePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.AudioRuntimeState.razor.cs");
        var audioPlaybackPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.AudioPlayback.razor.cs");
        var deepLinkPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.DeepLink.razor.cs");

        var markup = File.ReadAllText(homePath);

        Assert.DoesNotContain("@code", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("QrPreviewTour", markup, StringComparison.Ordinal);
        Assert.True(File.Exists(codeBehindPath), "Home.razor.cs should contain Home behavior so Home.razor stays focused on markup.");
        Assert.True(File.Exists(startupPath), "Home.Startup.razor.cs should isolate lifecycle/startup orchestration.");
        Assert.True(File.Exists(startupWorkPath), "Home.StartupWork.razor.cs should isolate deferred startup work orchestration.");
        Assert.True(File.Exists(mapRuntimePath), "Home.MapRuntime.razor.cs should isolate map render orchestration.");
        Assert.True(File.Exists(runtimePath), "Home.Runtime.razor.cs should isolate bridge/dispose runtime behavior.");
        Assert.True(File.Exists(runtimeDisposalPath), "Home.RuntimeDisposal.razor.cs should isolate teardown/dispose behavior.");
        Assert.True(File.Exists(audioRuntimePath), "Home.AudioRuntime.razor.cs should isolate audio runtime orchestration.");
        Assert.True(File.Exists(audioRuntimeStatePath), "Home.AudioRuntimeState.razor.cs should isolate audio cue restore/finalize behavior.");
        Assert.True(File.Exists(audioPlaybackPath), "Home.AudioPlayback.razor.cs should isolate audio playback controls.");
        Assert.True(File.Exists(deepLinkPath), "Home.DeepLink.razor.cs should isolate deep-link runtime orchestration.");

        var codeBehind = File.ReadAllText(codeBehindPath);
        var startupCode = File.ReadAllText(startupPath);
        var startupWorkCode = File.ReadAllText(startupWorkPath);
        var mapRuntimeCode = File.ReadAllText(mapRuntimePath);
        var runtimeCode = File.ReadAllText(runtimePath);
        var runtimeDisposalCode = File.ReadAllText(runtimeDisposalPath);
        var audioRuntimeCode = File.ReadAllText(audioRuntimePath);
        var audioRuntimeStateCode = File.ReadAllText(audioRuntimeStatePath);
        var audioPlaybackCode = File.ReadAllText(audioPlaybackPath);
        var deepLinkCode = File.ReadAllText(deepLinkPath);
        Assert.Contains("public partial class Home", codeBehind, StringComparison.Ordinal);
        Assert.Contains("IAsyncDisposable", codeBehind, StringComparison.Ordinal);
        Assert.Contains("OnAfterRenderAsync", startupCode, StringComparison.Ordinal);
        Assert.Contains("QueueStartupWork", startupWorkCode, StringComparison.Ordinal);
        Assert.Contains("RunStartupWorkAsync", startupWorkCode, StringComparison.Ordinal);
        Assert.Contains("RenderMapIfNeededAsync", mapRuntimeCode, StringComparison.Ordinal);
        Assert.Contains("[JSInvokable]", runtimeCode, StringComparison.Ordinal);
        Assert.DoesNotContain("private async Task ProcessPendingDeepLinkAsync", runtimeCode, StringComparison.Ordinal);
        Assert.DoesNotContain("private async Task PrepareSelectedPoiAudioAsync", runtimeCode, StringComparison.Ordinal);
        Assert.Contains("DisposeAsync", runtimeDisposalCode, StringComparison.Ordinal);
        Assert.Contains("PrepareSelectedPoiAudioAsync", audioRuntimeCode, StringComparison.Ordinal);
        Assert.Contains("RestorePreparedAudioState", audioRuntimeStateCode, StringComparison.Ordinal);
        Assert.Contains("TogglePlaybackAsync", audioPlaybackCode, StringComparison.Ordinal);
        Assert.Contains("ProcessPendingDeepLinkAsync", deepLinkCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_behavior_is_split_into_focused_partials()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var pageRoot = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages");
        var codeBehindPath = Path.Combine(pageRoot, "Home.razor.cs");
        var expectedPartials = new[]
        {
            ("Home.Startup.razor.cs", "OnAfterRenderAsync"),
            ("Home.StartupWork.razor.cs", "RunStartupWorkAsync"),
            ("Home.MapRuntime.razor.cs", "RenderMapIfNeededAsync"),
            ("Home.RuntimeDisposal.razor.cs", "DisposeAsync"),
            ("Home.MapControls.razor.cs", "ZoomMapInAsync"),
            ("Home.Content.razor.cs", "LoadContentAsync"),
            ("Home.ContentActions.razor.cs", "EnableLocationAsync"),
            ("Home.Navigation.razor.cs", "SwitchTabFromShell"),
            ("Home.NavigationPresentation.razor.cs", "VisitorNavigationPresentationFormatter"),
            ("Home.PoiNavigation.razor.cs", "OpenPoiDetailAsync"),
            ("Home.Qr.razor.cs", "VisitorQrPreviewSelector"),
            ("Home.Search.razor.cs", "OpenSearchOverlay"),
            ("Home.SearchResults.razor.cs", "GetSearchResultCount"),
            ("Home.Player.razor.cs", "OpenFullPlayer"),
            ("Home.PlayerControls.razor.cs", "SeekAudioAsync"),
            ("Home.PlayerLanguage.razor.cs", "SelectAudioLanguageAsync"),
            ("Home.PlayerPresentation.razor.cs", "GetCurrentAudioLanguageLabel"),
            ("Home.AudioRuntime.razor.cs", "PrepareSelectedPoiAudioAsync"),
            ("Home.AudioRuntimeState.razor.cs", "RestorePreparedAudioState"),
            ("Home.AudioPlayback.razor.cs", "TogglePlaybackAsync"),
            ("Home.DeepLink.razor.cs", "ProcessPendingDeepLinkAsync"),
            ("Home.Settings.razor.cs", "OpenSettingsScreen"),
            ("Home.SettingsPresentation.razor.cs", "VisitorSettingsPresentationFormatter"),
            ("Home.SettingsInsights.razor.cs", "GetListeningHistoryHeadline"),
            ("Home.SettingsActions.razor.cs", "SelectSettingsLanguageAsync"),
            ("Home.AudioSettingsActions.razor.cs", "SetAudioAutoPlayEnabledAsync"),
            ("Home.GpsSettingsActions.razor.cs", "SetGpsBackgroundTrackingEnabledAsync"),
            ("Home.CacheSettingsActions.razor.cs", "ClearCachedAudioItemsAsync"),
            ("Home.Profile.razor.cs", "VisitorProfilePresentationFormatter"),
            ("Home.ProfileActions.razor.cs", "SaveProfileDraftAsync"),
            ("Home.About.razor.cs", "GetAboutVersionLabel"),
            ("Home.Tours.razor.cs", "GetSelectedTourStopItems"),
            ("Home.TourPresentation.razor.cs", "VisitorTourPresentationFormatter"),
            ("Home.TourActions.razor.cs", "StartSelectedTourAsync"),
            ("Home.TourProgression.razor.cs", "AdvanceActiveTourAsync")
        };

        foreach (var (fileName, marker) in expectedPartials)
        {
            var path = Path.Combine(pageRoot, fileName);
            Assert.True(File.Exists(path), $"{fileName} should exist.");
            var source = File.ReadAllText(path);
            Assert.Contains("public partial class Home", source, StringComparison.Ordinal);
            Assert.Contains(marker, source, StringComparison.Ordinal);
        }

        var codeBehindLineCount = File.ReadAllLines(codeBehindPath).Length;
        Assert.True(codeBehindLineCount <= 80, $"Home.razor.cs should stay focused on state/core fields, but has {codeBehindLineCount} lines.");
        var runtimePath = Path.Combine(pageRoot, "Home.Runtime.razor.cs");
        var runtimeLineCount = File.ReadAllLines(runtimePath).Length;
        Assert.True(runtimeLineCount <= 70, $"Home.Runtime.razor.cs should stay focused on JS bridge callbacks, but has {runtimeLineCount} lines.");
        var runtimeDisposalPath = Path.Combine(pageRoot, "Home.RuntimeDisposal.razor.cs");
        var runtimeDisposalLineCount = File.ReadAllLines(runtimeDisposalPath).Length;
        Assert.True(runtimeDisposalLineCount <= 50, $"Home.RuntimeDisposal.razor.cs should stay focused on teardown/dispose behavior, but has {runtimeDisposalLineCount} lines.");
        var audioRuntimePath = Path.Combine(pageRoot, "Home.AudioRuntime.razor.cs");
        var audioRuntimeLineCount = File.ReadAllLines(audioRuntimePath).Length;
        Assert.True(audioRuntimeLineCount <= 60, $"Home.AudioRuntime.razor.cs should stay focused on audio cue preparation orchestration, but has {audioRuntimeLineCount} lines.");
        var audioRuntimeStatePath = Path.Combine(pageRoot, "Home.AudioRuntimeState.razor.cs");
        var audioRuntimeStateLineCount = File.ReadAllLines(audioRuntimeStatePath).Length;
        Assert.True(audioRuntimeStateLineCount <= 80, $"Home.AudioRuntimeState.razor.cs should stay focused on audio cue restore/finalize behavior, but has {audioRuntimeStateLineCount} lines.");
        var audioPlaybackPath = Path.Combine(pageRoot, "Home.AudioPlayback.razor.cs");
        var audioPlaybackLineCount = File.ReadAllLines(audioPlaybackPath).Length;
        Assert.True(audioPlaybackLineCount <= 70, $"Home.AudioPlayback.razor.cs should stay focused on audio playback controls, but has {audioPlaybackLineCount} lines.");
        var deepLinkPath = Path.Combine(pageRoot, "Home.DeepLink.razor.cs");
        var deepLinkLineCount = File.ReadAllLines(deepLinkPath).Length;
        Assert.True(deepLinkLineCount <= 70, $"Home.DeepLink.razor.cs should stay focused on deep-link orchestration, but has {deepLinkLineCount} lines.");
        var startupPath = Path.Combine(pageRoot, "Home.Startup.razor.cs");
        var startupLineCount = File.ReadAllLines(startupPath).Length;
        Assert.True(startupLineCount <= 70, $"Home.Startup.razor.cs should stay focused on lifecycle hooks, but has {startupLineCount} lines.");
        var startupWorkPath = Path.Combine(pageRoot, "Home.StartupWork.razor.cs");
        var startupWorkLineCount = File.ReadAllLines(startupWorkPath).Length;
        Assert.True(startupWorkLineCount <= 90, $"Home.StartupWork.razor.cs should stay focused on deferred startup work, but has {startupWorkLineCount} lines.");
        var mapRuntimePath = Path.Combine(pageRoot, "Home.MapRuntime.razor.cs");
        var mapRuntimeLineCount = File.ReadAllLines(mapRuntimePath).Length;
        Assert.True(mapRuntimeLineCount <= 70, $"Home.MapRuntime.razor.cs should stay focused on map render orchestration, but has {mapRuntimeLineCount} lines.");
        var mapControlsPath = Path.Combine(pageRoot, "Home.MapControls.razor.cs");
        var mapControlsLineCount = File.ReadAllLines(mapControlsPath).Length;
        Assert.True(mapControlsLineCount <= 40, $"Home.MapControls.razor.cs should stay focused on map zoom controls, but has {mapControlsLineCount} lines.");
        var contentPath = Path.Combine(pageRoot, "Home.Content.razor.cs");
        var contentLineCount = File.ReadAllLines(contentPath).Length;
        Assert.True(contentLineCount <= 70, $"Home.Content.razor.cs should stay focused on content load orchestration, but has {contentLineCount} lines.");
        var contentActionsPath = Path.Combine(pageRoot, "Home.ContentActions.razor.cs");
        var contentActionsLineCount = File.ReadAllLines(contentActionsPath).Length;
        Assert.True(contentActionsLineCount <= 70, $"Home.ContentActions.razor.cs should stay focused on user-triggered content actions, but has {contentActionsLineCount} lines.");
        var navigationPath = Path.Combine(pageRoot, "Home.Navigation.razor.cs");
        var navigationLineCount = File.ReadAllLines(navigationPath).Length;
        Assert.True(navigationLineCount <= 90, $"Home.Navigation.razor.cs should stay focused on shell navigation, but has {navigationLineCount} lines.");
        var navigationPresentationPath = Path.Combine(pageRoot, "Home.NavigationPresentation.razor.cs");
        var navigationPresentationLineCount = File.ReadAllLines(navigationPresentationPath).Length;
        Assert.True(navigationPresentationLineCount <= 55, $"Home.NavigationPresentation.razor.cs should stay focused on view-state wrappers, but has {navigationPresentationLineCount} lines.");
        var qrPath = Path.Combine(pageRoot, "Home.Qr.razor.cs");
        var qrLineCount = File.ReadAllLines(qrPath).Length;
        Assert.True(qrLineCount <= 55, $"Home.Qr.razor.cs should stay focused on QR modal actions, but has {qrLineCount} lines.");
        var poiNavigationPath = Path.Combine(pageRoot, "Home.PoiNavigation.razor.cs");
        var poiNavigationLineCount = File.ReadAllLines(poiNavigationPath).Length;
        Assert.True(poiNavigationLineCount <= 60, $"Home.PoiNavigation.razor.cs should stay focused on POI navigation actions, but has {poiNavigationLineCount} lines.");
        var searchPath = Path.Combine(pageRoot, "Home.Search.razor.cs");
        var searchLineCount = File.ReadAllLines(searchPath).Length;
        Assert.True(searchLineCount <= 50, $"Home.Search.razor.cs should stay focused on search overlay actions, but has {searchLineCount} lines.");
        var searchResultsPath = Path.Combine(pageRoot, "Home.SearchResults.razor.cs");
        var searchResultsLineCount = File.ReadAllLines(searchResultsPath).Length;
        Assert.True(searchResultsLineCount <= 40, $"Home.SearchResults.razor.cs should stay focused on search result composition, but has {searchResultsLineCount} lines.");
        var settingsPath = Path.Combine(pageRoot, "Home.Settings.razor.cs");
        var settingsLineCount = File.ReadAllLines(settingsPath).Length;
        Assert.True(settingsLineCount <= 80, $"Home.Settings.razor.cs should stay focused on settings shell/navigation, but has {settingsLineCount} lines.");
        var settingsPresentationPath = Path.Combine(pageRoot, "Home.SettingsPresentation.razor.cs");
        var settingsPresentationLineCount = File.ReadAllLines(settingsPresentationPath).Length;
        Assert.True(settingsPresentationLineCount <= 45, $"Home.SettingsPresentation.razor.cs should stay focused on summary wrappers, but has {settingsPresentationLineCount} lines.");
        var settingsInsightsPath = Path.Combine(pageRoot, "Home.SettingsInsights.razor.cs");
        var settingsInsightsLineCount = File.ReadAllLines(settingsInsightsPath).Length;
        Assert.True(settingsInsightsLineCount <= 45, $"Home.SettingsInsights.razor.cs should stay focused on stats/history wrappers, but has {settingsInsightsLineCount} lines.");
        var settingsActionsPath = Path.Combine(pageRoot, "Home.SettingsActions.razor.cs");
        var settingsActionsLineCount = File.ReadAllLines(settingsActionsPath).Length;
        Assert.True(settingsActionsLineCount <= 40, $"Home.SettingsActions.razor.cs should stay focused on shared settings actions, but has {settingsActionsLineCount} lines.");
        var audioSettingsActionsPath = Path.Combine(pageRoot, "Home.AudioSettingsActions.razor.cs");
        var audioSettingsActionsLineCount = File.ReadAllLines(audioSettingsActionsPath).Length;
        Assert.True(audioSettingsActionsLineCount <= 80, $"Home.AudioSettingsActions.razor.cs should stay focused on audio settings actions, but has {audioSettingsActionsLineCount} lines.");
        var gpsSettingsActionsPath = Path.Combine(pageRoot, "Home.GpsSettingsActions.razor.cs");
        var gpsSettingsActionsLineCount = File.ReadAllLines(gpsSettingsActionsPath).Length;
        Assert.True(gpsSettingsActionsLineCount <= 50, $"Home.GpsSettingsActions.razor.cs should stay focused on GPS settings actions, but has {gpsSettingsActionsLineCount} lines.");
        var cacheSettingsActionsPath = Path.Combine(pageRoot, "Home.CacheSettingsActions.razor.cs");
        var cacheSettingsActionsLineCount = File.ReadAllLines(cacheSettingsActionsPath).Length;
        Assert.True(cacheSettingsActionsLineCount <= 40, $"Home.CacheSettingsActions.razor.cs should stay focused on cache settings actions, but has {cacheSettingsActionsLineCount} lines.");
        var profilePath = Path.Combine(pageRoot, "Home.Profile.razor.cs");
        var profileLineCount = File.ReadAllLines(profilePath).Length;
        Assert.True(profileLineCount <= 60, $"Home.Profile.razor.cs should stay focused on profile presentation wrappers, but has {profileLineCount} lines.");
        var profileActionsPath = Path.Combine(pageRoot, "Home.ProfileActions.razor.cs");
        var profileActionsLineCount = File.ReadAllLines(profileActionsPath).Length;
        Assert.True(profileActionsLineCount <= 60, $"Home.ProfileActions.razor.cs should stay focused on profile draft actions, but has {profileActionsLineCount} lines.");
        var toursPath = Path.Combine(pageRoot, "Home.Tours.razor.cs");
        var toursLineCount = File.ReadAllLines(toursPath).Length;
        Assert.True(toursLineCount <= 140, $"Home.Tours.razor.cs should stay focused on tour detail composition, but has {toursLineCount} lines.");
        var tourActionsPath = Path.Combine(pageRoot, "Home.TourActions.razor.cs");
        var tourActionsLineCount = File.ReadAllLines(tourActionsPath).Length;
        Assert.True(tourActionsLineCount <= 55, $"Home.TourActions.razor.cs should stay focused on tour start/select actions, but has {tourActionsLineCount} lines.");
        var tourProgressionPath = Path.Combine(pageRoot, "Home.TourProgression.razor.cs");
        var tourProgressionLineCount = File.ReadAllLines(tourProgressionPath).Length;
        Assert.True(tourProgressionLineCount <= 60, $"Home.TourProgression.razor.cs should stay focused on active tour progression, but has {tourProgressionLineCount} lines.");
        var playerPath = Path.Combine(pageRoot, "Home.Player.razor.cs");
        var playerLineCount = File.ReadAllLines(playerPath).Length;
        Assert.True(playerLineCount <= 60, $"Home.Player.razor.cs should stay focused on player shell state, but has {playerLineCount} lines.");
        var playerControlsPath = Path.Combine(pageRoot, "Home.PlayerControls.razor.cs");
        var playerControlsLineCount = File.ReadAllLines(playerControlsPath).Length;
        Assert.True(playerControlsLineCount <= 55, $"Home.PlayerControls.razor.cs should stay focused on transport controls, but has {playerControlsLineCount} lines.");
        var playerLanguagePath = Path.Combine(pageRoot, "Home.PlayerLanguage.razor.cs");
        var playerLanguageLineCount = File.ReadAllLines(playerLanguagePath).Length;
        Assert.True(playerLanguageLineCount <= 50, $"Home.PlayerLanguage.razor.cs should stay focused on player language switching, but has {playerLanguageLineCount} lines.");
    }

    [Fact]
    public void Mobile_diagnostics_do_not_write_to_console_in_runtime_builds()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var diagnosticsPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorMobileDiagnostics.cs");

        var source = File.ReadAllText(diagnosticsPath);

        Assert.DoesNotContain("Console.WriteLine", source, StringComparison.Ordinal);
        Assert.Contains("[Conditional(\"DEBUG\")]", source, StringComparison.Ordinal);
        Assert.Contains("[Conditional(\"SMOKE\")]", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_dedicated_map_tab()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var codeBehindPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.MapRuntime.razor.cs");

        var markup = File.ReadAllText(homePath);
        var codeBehind = File.ReadAllText(codeBehindPath);

        Assert.Contains("VisitorTab.Map", markup, StringComparison.Ordinal);
        Assert.Contains("Bản đồ", markup, StringComparison.Ordinal);
        Assert.Contains("CurrentTab != VisitorTab.Map", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_setup_starts_at_language_without_auth_or_guest_gate()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorSetupFlow.razor");

        var markup = File.ReadAllText(homePath);

        Assert.DoesNotContain("case VisitorIntroStep.Welcome:", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorAuthScreen", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ContinueFromSetupAsync", markup, StringComparison.Ordinal);
        Assert.Contains("case VisitorIntroStep.Language:", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_language_setup_is_first_step_before_permissions()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorSetupFlow.razor");

        var markup = File.ReadAllText(homePath);
        var languageSection = ExtractSection(markup, "case VisitorIntroStep.Language:", "case VisitorIntroStep.Permissions:");

        Assert.Contains("language-grid", languageSection, StringComparison.Ordinal);
        Assert.Contains("Chọn ngôn ngữ audio ưu tiên.", languageSection, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorAuthScreen", languageSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_permissions_copy_no_longer_mentions_guest_mode()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorSetupFlow.razor");

        var markup = File.ReadAllText(homePath);
        var startIndex = markup.IndexOf("case VisitorIntroStep.Permissions:", StringComparison.Ordinal);
        var endIndex = markup.IndexOf("</section>", startIndex, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, "Could not find permissions section.");
        Assert.True(endIndex > startIndex, "Could not find end of permissions section.");
        var permissionsSection = markup[startIndex..endIndex];

        Assert.DoesNotContain("guest", permissionsSection, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("đăng nhập", permissionsSection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mobile_home_ready_state_no_longer_contains_auth_overlay_or_guest_snackbar()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.DoesNotContain("OpenAuthOverlay", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("CloseAuthOverlay", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_isAuthOverlayOpen", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("guest-auth-snackbar", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_settings_include_profile_overview_and_subscreen_navigation()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var settingsOverviewPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorSettingsOverviewScreen.razor");

        var markup = File.ReadAllText(homePath);
        var settingsOverviewMarkup = File.ReadAllText(settingsOverviewPath);

        Assert.Contains("VisitorSettingsOverviewScreen", markup, StringComparison.Ordinal);
        Assert.Contains("settings-profile-card", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-stat-grid", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-nav-list", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenAudio", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenGps", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenCache", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenHistory", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenAbout", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenProfile", settingsOverviewMarkup, StringComparison.Ordinal);
        Assert.Contains("VisitorAudioSettingsScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorGpsSettingsScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorCacheManagerScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorListenHistoryScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorAboutScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorEditProfileScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_five_audio_speed_presets_for_settings_and_full_player()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor.cs");

        var source = File.ReadAllText(homePath);

        Assert.Contains("private static readonly double[] AudioSpeedOptions = [0.75d, 1d, 1.25d, 1.5d, 2d];", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_tracks_content_loading_and_passes_it_to_discover_and_tour_sections()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var codeBehindPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor.cs");
        var contentPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.Content.razor.cs");

        var markup = File.ReadAllText(homePath);
        var codeBehind = File.ReadAllText(codeBehindPath);
        var contentCode = File.ReadAllText(contentPath);

        Assert.Contains("private bool _isContentLoading;", codeBehind, StringComparison.Ordinal);
        Assert.Contains("IsLoading=\"@_isContentLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("_isContentLoading = true;", contentCode, StringComparison.Ordinal);
        Assert.Contains("_isContentLoading = false;", contentCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_defers_startup_sync_until_after_first_render()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.Startup.razor.cs");
        var startupWorkPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.StartupWork.razor.cs");

        var source = File.ReadAllText(homePath);
        var startupWorkSource = File.ReadAllText(startupWorkPath);
        var initializedSection = ExtractSection(source, "protected override void OnInitialized()", "protected override async Task OnAfterRenderAsync(bool firstRender)");
        var afterRenderSection = source[source.IndexOf("protected override async Task OnAfterRenderAsync(bool firstRender)", StringComparison.Ordinal)..];

        Assert.DoesNotContain("await LoadContentAsync();", initializedSection, StringComparison.Ordinal);
        Assert.DoesNotContain("await ProcessPendingDeepLinkAsync();", initializedSection, StringComparison.Ordinal);
        Assert.Contains("QueueStartupWork();", afterRenderSection, StringComparison.Ordinal);
        Assert.Contains("RunStartupWorkAsync", startupWorkSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_screen_uses_fullscreen_overlay_and_bottom_sheet_layout()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorMapScreen.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = markup;

        Assert.Contains("map-screen", mapSection, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay", mapSection, StringComparison.Ordinal);
        Assert.Contains("map-top-controls", mapSection, StringComparison.Ordinal);
        Assert.Contains("map-category-rail", mapSection, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay--sheet-open", mapSection, StringComparison.Ordinal);
        Assert.DoesNotContain("map-top-search", mapSection, StringComparison.Ordinal);
        Assert.Contains("map-fab-rail", mapSection, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__grabber", mapSection, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__meta-chips", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_screen_includes_qr_fab_and_preview_modal()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorMapScreen.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = markup;

        Assert.Contains("qr-fab", mapSection, StringComparison.Ordinal);
        Assert.Contains("qr-modal", mapSection, StringComparison.Ordinal);
        Assert.Contains("qr-target-btn", mapSection, StringComparison.Ordinal);
        Assert.Contains("OnTriggerQrPreview", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_screen_includes_offline_banner_and_geofence_toast()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorMapScreen.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = markup;

        Assert.Contains("offline-banner", mapSection, StringComparison.Ordinal);
        Assert.Contains("geofence-toast", mapSection, StringComparison.Ordinal);
        Assert.Contains("GeofenceToastMessageProvider", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_uses_filtered_pois_for_full_marker_set()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.MapRuntime.razor.cs");

        var source = File.ReadAllText(homePath);

        Assert.Contains("VisitorMapSnapshotBuilder.Build(_state.FilteredPois, _state.SelectedPoiId, _state.CurrentLocation)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorMapSnapshotBuilder.Build(_state.FeaturedPois, _state.SelectedPoiId, _state.CurrentLocation)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_dedicated_search_screen_overlay()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var mapPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorMapScreen.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = File.ReadAllText(mapPath);

        Assert.Contains("VisitorSearchScreen", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSearchOverlay", markup, StringComparison.Ordinal);
        Assert.Contains("CloseSearchOverlay", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("readonly", mapSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Tìm điểm tham quan...", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_full_audio_player_overlay()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("VisitorFullPlayerScreen", markup, StringComparison.Ordinal);
        Assert.Contains("OpenFullPlayer", markup, StringComparison.Ordinal);
        Assert.Contains("CloseFullPlayer", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_qr_flow_routes_poi_scans_into_discover_detail_screen()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var deepLinkPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.DeepLinkNavigation.razor.cs");

        var source = File.ReadAllText(deepLinkPath);

        Assert.Contains("VisitorTab.Discover", source, StringComparison.Ordinal);
        Assert.Contains("_discoverPoiDetailId", source, StringComparison.Ordinal);
        Assert.Contains("_state.PreviewPoi", source, StringComparison.Ordinal);
    }

    private static string ExtractSection(string markup, string startMarker, string endMarker)
    {
        var startIndex = markup.IndexOf(startMarker, StringComparison.Ordinal);
        var endIndex = markup.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.True(startIndex >= 0, $"Could not find start marker '{startMarker}'.");
        Assert.True(endIndex > startIndex, $"Could not find end marker '{endMarker}' after '{startMarker}'.");

        return markup[startIndex..endIndex];
    }
}
