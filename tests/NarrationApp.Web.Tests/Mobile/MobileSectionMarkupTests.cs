using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileSectionMarkupTests
{
    [Fact]
    public void Mobile_home_uses_dedicated_section_components_for_discover_and_tour_flows()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("VisitorDiscoverScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorPoiDetailScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorTourListScreen", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorTourDetailScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_dedicated_setup_and_map_section_components()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("VisitorSetupFlow", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorMapScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_dedicated_settings_overview_component()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");

        var markup = File.ReadAllText(homePath);

        Assert.Contains("VisitorSettingsOverviewScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_setup_flow_component_keeps_language_and_permission_hooks()
    {
        var markup = ReadSectionMarkup("VisitorSetupFlow.razor");

        Assert.Contains("case VisitorIntroStep.Language:", markup, StringComparison.Ordinal);
        Assert.Contains("case VisitorIntroStep.Permissions:", markup, StringComparison.Ordinal);
        Assert.Contains("setup-phone-chrome", markup, StringComparison.Ordinal);
        Assert.Contains("setup-statusbar", markup, StringComparison.Ordinal);
        Assert.Contains("setup-dynamic-island", markup, StringComparison.Ordinal);
        Assert.Contains("setup-card--language", markup, StringComparison.Ordinal);
        Assert.Contains("setup-stack--language", markup, StringComparison.Ordinal);
        Assert.Contains("setup-card--permissions", markup, StringComparison.Ordinal);
        Assert.Contains("setup-stack--permissions", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-list", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-option__code", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-option__indicator", markup, StringComparison.Ordinal);
        Assert.Contains("setup-permission-icon", markup, StringComparison.Ordinal);
        Assert.Contains("Tiếp tục", markup, StringComparison.Ordinal);
        Assert.Contains("Cho phép vị trí", markup, StringComparison.Ordinal);
        Assert.Contains("Bỏ qua — Dùng QR / thủ công", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorAuthScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_screen_component_keeps_fullscreen_overlay_without_in_app_qr_hooks()
    {
        var markup = ReadSectionMarkup("VisitorMapScreen.razor");

        Assert.Contains("map-screen", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-controls", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-search", markup, StringComparison.Ordinal);
        Assert.Contains("map-category-rail", markup, StringComparison.Ordinal);
        Assert.Contains("notification-panel__surface", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("qr-fab", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("qr-modal", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__grabber", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__audio-status", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__queue", markup, StringComparison.Ordinal);
        Assert.Contains("QueuedPoiStatus", markup, StringComparison.Ordinal);
        Assert.Contains("Dẫn tới đây", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenDirections", markup, StringComparison.Ordinal);
        Assert.Contains("Xem chi tiết", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenPoiDetail", markup, StringComparison.Ordinal);
        Assert.Contains("geofence-toast", markup, StringComparison.Ordinal);
        Assert.Contains("geofence-toast__queue", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_settings_overview_component_keeps_profile_language_and_navigation_hooks()
    {
        var markup = ReadSectionMarkup("VisitorSettingsOverviewScreen.razor");

        Assert.Contains("settings-screen", markup, StringComparison.Ordinal);
        Assert.Contains("settings-profile-card", markup, StringComparison.Ordinal);
        Assert.Contains("settings-stat-grid", markup, StringComparison.Ordinal);
        Assert.Contains("Thiết bị hiện tại", markup, StringComparison.Ordinal);
        Assert.Contains("settings-language-strip", markup, StringComparison.Ordinal);
        Assert.Contains("settings-nav-list", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenAudio", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenGps", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenCache", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenHistory", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenAbout", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("OnOpenProfile", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Hồ sơ cục bộ", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_discover_screen_matches_sample_strict_layout_hooks()
    {
        var markup = ReadSectionMarkup("VisitorDiscoverScreen.razor");

        Assert.Contains("discover-screen", markup, StringComparison.Ordinal);
        Assert.Contains("IsLoading", markup, StringComparison.Ordinal);
        Assert.Contains("discover-refresh-button", markup, StringComparison.Ordinal);
        Assert.Contains("OnRefresh", markup, StringComparison.Ordinal);
        Assert.Contains("discover-search", markup, StringComparison.Ordinal);
        Assert.Contains("discover-search--strict", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenSearch", markup, StringComparison.Ordinal);
        Assert.Contains("readonly", markup, StringComparison.Ordinal);
        Assert.Contains("discover-chip-row", markup, StringComparison.Ordinal);
        Assert.Contains("discover-list--stagger", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card--skeleton", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__skeleton", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__media", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__image", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__image-fallback", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__topline", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__title", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__summary", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__status", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("discover-refresh-indicator", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_poi_detail_screen_matches_sample_strict_layout_hooks()
    {
        var markup = ReadSectionMarkup("VisitorPoiDetailScreen.razor");

        Assert.Contains("poi-detail-screen", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-hero", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-headline__eyebrow", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-headline__chips", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-stats", markup, StringComparison.Ordinal);
        Assert.Contains("poi-audio-panel", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-audio-card", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-audio-card__wave", markup, StringComparison.Ordinal);
        Assert.Contains("poi-language-pills", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-audio-heading", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-audio-heading__title", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-audio-heading__action", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-headline__distance", markup, StringComparison.Ordinal);
        Assert.Contains("poi-audio-panel__player", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-copy", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-transcript", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-related", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-related__header", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-related__item", markup, StringComparison.Ordinal);
        Assert.Contains("poi-detail-sticky-cta", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenFullPlayer", markup, StringComparison.Ordinal);
        Assert.Contains("Poi.GeofenceRadiusMeters", markup, StringComparison.Ordinal);
        Assert.Contains("GetPriorityScore()", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_tour_screens_match_sample_strict_layout_hooks()
    {
        var listMarkup = ReadSectionMarkup("VisitorTourListScreen.razor");
        var detailMarkup = ReadSectionMarkup("VisitorTourDetailScreen.razor");

        Assert.Contains("tour-list-screen", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-list-header__copy", listMarkup, StringComparison.Ordinal);
        Assert.Contains("IsLoading", listMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("tour-guest-prompt", listMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowGuestPrompt", listMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("OnOpenAuth", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-showcase-list--stagger", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-showcase-card", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-showcase-card--skeleton", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-showcase-card__summary", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-showcase-card__description", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-list-footer-stats", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-list-footer-stats__item", listMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-detail-screen", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-detail-hero__pill", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-detail-hero__copy", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-detail-overview", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-detail-sheet-actions", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-progress-track", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-progress-track__summary", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-stop-timeline", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("tour-stop-timeline__copy", detailMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_auth_screen_component_has_been_removed_from_mobile_flow()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var authScreenPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorAuthScreen.razor");

        Assert.False(File.Exists(authScreenPath), $"Expected auth screen to be removed, but found '{authScreenPath}'.");
    }

    [Fact]
    public void Mobile_settings_screens_match_reviewed_subscreen_layout_hooks()
    {
        var audioMarkup = ReadSectionMarkup("VisitorAudioSettingsScreen.razor");
        var gpsMarkup = ReadSectionMarkup("VisitorGpsSettingsScreen.razor");
        var cacheMarkup = ReadSectionMarkup("VisitorCacheManagerScreen.razor");
        var historyMarkup = ReadSectionMarkup("VisitorListenHistoryScreen.razor");
        var aboutMarkup = ReadSectionMarkup("VisitorAboutScreen.razor");

        Assert.Contains("settings-detail-screen", audioMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-toggle-row", audioMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-segment", audioMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-speed-grid", audioMarkup, StringComparison.Ordinal);

        Assert.Contains("settings-detail-screen", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-gps-status", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-toggle-row", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-segment", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("settings-debug-log", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("GeofenceDebugEvents", gpsMarkup, StringComparison.Ordinal);
        Assert.Contains("debug-log-item", gpsMarkup, StringComparison.Ordinal);

        Assert.Contains("settings-detail-screen", cacheMarkup, StringComparison.Ordinal);
        Assert.Contains("cache-summary-card", cacheMarkup, StringComparison.Ordinal);
        Assert.Contains("cache-item", cacheMarkup, StringComparison.Ordinal);
        Assert.Contains("OnClearAll", cacheMarkup, StringComparison.Ordinal);

        Assert.Contains("settings-detail-screen", historyMarkup, StringComparison.Ordinal);
        Assert.Contains("history-day", historyMarkup, StringComparison.Ordinal);
        Assert.Contains("history-entry", historyMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenPoi", historyMarkup, StringComparison.Ordinal);

        Assert.Contains("settings-detail-screen", aboutMarkup, StringComparison.Ordinal);
        Assert.Contains("about-info-card", aboutMarkup, StringComparison.Ordinal);
        Assert.Contains("about-link-list", aboutMarkup, StringComparison.Ordinal);
        Assert.Contains("NarrationApp Mobile", aboutMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Build dành cho visitor", aboutMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_profile_editor_screen_component_is_retired_and_no_longer_contains_editor_ui()
    {
        var retiredScreenPath = GetSectionPath("VisitorEditProfileScreen.razor");

        Assert.False(File.Exists(retiredScreenPath), "VisitorEditProfileScreen.razor should stay retired instead of keeping hidden editor UI.");
    }

    [Fact]
    public void Mobile_search_screen_matches_sample_strict_layout_hooks()
    {
        var markup = ReadSectionMarkup("VisitorSearchScreen.razor");

        Assert.Contains("search-screen", markup, StringComparison.Ordinal);
        Assert.Contains("search-top", markup, StringComparison.Ordinal);
        Assert.Contains("search-top__field--strict", markup, StringComparison.Ordinal);
        Assert.Contains("search-screen__summary", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-count", markup, StringComparison.Ordinal);
        Assert.Contains("search-section", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item__eyebrow", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item__summary", markup, StringComparison.Ordinal);
        Assert.Contains("search-suggestion-chips", markup, StringComparison.Ordinal);
        Assert.Contains("<mark>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_full_player_screen_matches_sample_strict_layout_hooks()
    {
        var markup = ReadSectionMarkup("VisitorFullPlayerScreen.razor");

        Assert.Contains("full-player-screen", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-hero", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-summary__meta", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-progress", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-wave", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-controls", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-control--primary", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-transcript", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-transcript__surface", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_home_uses_polished_auxiliary_overlay_shells()
    {
        var markup = ReadSectionMarkup("VisitorMapScreen.razor");

        Assert.Contains("notification-panel__surface", markup, StringComparison.Ordinal);
        Assert.Contains("notification-panel__copy", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("qr-modal__panel", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("guest-auth-snackbar__copy", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("auth-overlay__card", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_layout_removes_top_search_and_uses_sheet_safe_overlay_hooks()
    {
        var markup = ReadSectionMarkup("VisitorMapScreen.razor");

        Assert.Contains("map-top-controls", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-search", markup, StringComparison.Ordinal);
        Assert.Contains("map-category-rail", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay--sheet-open", markup, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"Tìm theo tên, danh mục", markup, StringComparison.Ordinal);
    }

    private static string ReadSectionMarkup(string fileName)
    {
        return File.ReadAllText(GetSectionPath(fileName));
    }

    private static string GetSectionPath(string fileName)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", fileName);
    }
}
