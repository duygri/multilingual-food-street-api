using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class HomeMarkupTests
{
    [Fact]
    public void Mobile_home_uses_dedicated_map_tab()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("TouristTab.Map", markup, StringComparison.Ordinal);
        Assert.Contains("Bản đồ", markup, StringComparison.Ordinal);
        Assert.Contains("CurrentTab != TouristTab.Map", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_setup_offers_guest_and_tourist_actions_before_ready()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("TouristAuthScreen", markup, StringComparison.Ordinal);
        Assert.Contains("ContinueFromSetupAsync", markup, StringComparison.Ordinal);
        Assert.Contains("SubmitWelcomeAuthAsync", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_keeps_mode_selection_separate_from_language_selection()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var welcomeSection = ExtractSection(markup, "case TouristIntroStep.Welcome:", "case TouristIntroStep.Language:");
        var languageSection = ExtractSection(markup, "case TouristIntroStep.Language:", "case TouristIntroStep.Permissions:");

        Assert.Contains("TouristAuthScreen", welcomeSection, StringComparison.Ordinal);
        Assert.DoesNotContain("language-grid", welcomeSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Ngôn ngữ hiện tại", welcomeSection, StringComparison.Ordinal);

        Assert.Contains("language-grid", languageSection, StringComparison.Ordinal);
        Assert.Contains("Chọn ngôn ngữ audio ưu tiên.", languageSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_welcome_screen_uses_segmented_auth_without_social_login()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var welcomeSection = ExtractSection(markup, "case TouristIntroStep.Welcome:", "case TouristIntroStep.Language:");

        Assert.Contains("TouristAuthScreen", welcomeSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Google", welcomeSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Apple", welcomeSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_auth_overlay_and_guest_registration_snackbar_after_setup()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("OpenAuthOverlay", markup, StringComparison.Ordinal);
        Assert.Contains("CloseAuthOverlay", markup, StringComparison.Ordinal);
        Assert.Contains("_isAuthOverlayOpen", markup, StringComparison.Ordinal);
        Assert.Contains("guest-auth-snackbar", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_settings_include_profile_overview_and_subscreen_navigation()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("settings-profile-card", markup, StringComparison.Ordinal);
        Assert.Contains("settings-stat-grid", markup, StringComparison.Ordinal);
        Assert.Contains("settings-nav-list", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.Audio)", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.Gps)", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.Cache)", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.History)", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.About)", markup, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsScreen(TouristSettingsScreen.Profile)", markup, StringComparison.Ordinal);
        Assert.Contains("TouristAudioSettingsScreen", markup, StringComparison.Ordinal);
        Assert.Contains("TouristGpsSettingsScreen", markup, StringComparison.Ordinal);
        Assert.Contains("TouristCacheManagerScreen", markup, StringComparison.Ordinal);
        Assert.Contains("TouristListenHistoryScreen", markup, StringComparison.Ordinal);
        Assert.Contains("TouristAboutScreen", markup, StringComparison.Ordinal);
        Assert.Contains("TouristEditProfileScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_five_audio_speed_presets_for_settings_and_full_player()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("private static readonly double[] AudioSpeedOptions = [0.75d, 1d, 1.25d, 1.5d, 2d];", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_tracks_content_loading_and_passes_it_to_discover_and_tour_sections()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("private bool _isContentLoading;", markup, StringComparison.Ordinal);
        Assert.Contains("IsLoading=\"@_isContentLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("_isContentLoading = true;", markup, StringComparison.Ordinal);
        Assert.Contains("_isContentLoading = false;", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_screen_uses_fullscreen_overlay_and_bottom_sheet_layout()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = ExtractSection(markup, "if (_state.CurrentTab == TouristTab.Map)", "else if (_state.CurrentTab == TouristTab.Discover)");

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
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = ExtractSection(markup, "if (_state.CurrentTab == TouristTab.Map)", "else if (_state.CurrentTab == TouristTab.Discover)");

        Assert.Contains("qr-fab", mapSection, StringComparison.Ordinal);
        Assert.Contains("qr-modal", mapSection, StringComparison.Ordinal);
        Assert.Contains("qr-target-btn", mapSection, StringComparison.Ordinal);
        Assert.Contains("TriggerQrPreviewAsync", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_map_screen_includes_offline_banner_and_geofence_toast()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = ExtractSection(markup, "if (_state.CurrentTab == TouristTab.Map)", "else if (_state.CurrentTab == TouristTab.Discover)");

        Assert.Contains("offline-banner", mapSection, StringComparison.Ordinal);
        Assert.Contains("geofence-toast", mapSection, StringComparison.Ordinal);
        Assert.Contains("GetGeofenceToastMessage", mapSection, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_dedicated_search_screen_overlay()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);
        var mapSection = ExtractSection(markup, "if (_state.CurrentTab == TouristTab.Map)", "else if (_state.CurrentTab == TouristTab.Discover)");

        Assert.Contains("TouristSearchScreen", markup, StringComparison.Ordinal);
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

        Assert.Contains("TouristFullPlayerScreen", markup, StringComparison.Ordinal);
        Assert.Contains("OpenFullPlayer", markup, StringComparison.Ordinal);
        Assert.Contains("CloseFullPlayer", markup, StringComparison.Ordinal);
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
