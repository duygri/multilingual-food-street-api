using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileSectionMarkupTests
{
    [Fact]
    public void Mobile_discover_composes_one_search_city_lens_theme_chips_and_ordered_story_cards()
    {
        var markup = ReadSectionMarkup("VisitorDiscoverScreen.razor");

        Assert.Equal(1, CountOccurrences(markup, "class=\"discover-search"));
        Assert.Contains("class=\"theme-chips\"", markup, StringComparison.Ordinal);
        Assert.Contains("@foreach (var category in Categories)", markup, StringComparison.Ordinal);
        Assert.Contains("OnSelectCategory.InvokeAsync(category.Id)", markup, StringComparison.Ordinal);
        Assert.Contains("<VisitorCityLens", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenMap=\"OnOpenMap\"", markup, StringComparison.Ordinal);
        Assert.Contains("discover-sync-banner", markup, StringComparison.Ordinal);
        Assert.Contains("@foreach (var poi in Pois)", markup, StringComparison.Ordinal);
        Assert.Contains("<VisitorStoryCard", markup, StringComparison.Ordinal);
        Assert.Contains("OnListen=\"OnSelectPoi\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_city_lens_is_one_accessible_map_button_with_a_separate_static_preview_container()
    {
        var markup = ReadSectionMarkup("VisitorCityLens.razor");

        Assert.Equal(1, CountOccurrences(markup, "<button"));
        Assert.Contains("aria-label=\"@Text.Pick(", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"() => OnOpenMap.InvokeAsync()\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"city-lens-map\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("visitor-map-marker", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_story_card_shows_honest_metadata_and_one_primary_listen_affordance()
    {
        var markup = ReadSectionMarkup("VisitorStoryCard.razor");

        Assert.Contains("Poi.ImageUrl", markup, StringComparison.Ordinal);
        Assert.Contains("<VisitorIcon Name=\"@GetPoiIcon()\" />", markup, StringComparison.Ordinal);
        Assert.Contains("@if (ShowCityLabel)", markup, StringComparison.Ordinal);
        Assert.Contains("TP.HCM", markup, StringComparison.Ordinal);
        Assert.Contains("ShowCityLabel => !string.IsNullOrWhiteSpace(Poi.District)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("!Poi.District.Contains(\"TP.HCM\"", markup, StringComparison.Ordinal);
        Assert.Contains("@if (Poi.HasReliableDistance)", markup, StringComparison.Ordinal);
        Assert.Contains("Poi.AudioDuration", markup, StringComparison.Ordinal);
        Assert.Contains("<VisitorIcon Name=\"audio\" />", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("▶", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("◉", markup, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(markup, "story-card__listen"));
        Assert.Contains("Text.Pick(\"Listen\", \"Nghe câu chuyện\")", markup, StringComparison.Ordinal);
        Assert.Contains("OnListen.InvokeAsync(Poi.Id)", markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("VisitorDiscoverScreen.razor", "<VisitorIcon Name=\"@GetCategoryIcon(category.Id)\" />", "@GetCategoryIcon(category.Id)</span>")]
    [InlineData("VisitorStoryCard.razor", "<VisitorIcon Name=\"@GetPoiIcon()\" />", "@GetPoiIcon()</span>")]
    [InlineData("VisitorSearchScreen.razor", "<VisitorIcon Name=\"@GetCategoryIcon(category.Id)\" />", "@GetCategoryIcon(category.Id)</span>")]
    [InlineData("VisitorSearchScreen.razor", "<VisitorIcon Name=\"@GetPoiIcon(poi)\" />", "@GetPoiIcon(poi)</span>")]
    [InlineData("VisitorPoiDetailScreen.razor", "<VisitorIcon Name=\"@GetPoiIcon()\" />", ">@GetPoiIcon()</div>")]
    [InlineData("VisitorPoiDetailScreen.razor", "<VisitorIcon Name=\"@GetRelatedPoiIcon(relatedPoi)\" />", "@GetRelatedPoiIcon(relatedPoi)</span>")]
    [InlineData("VisitorFullPlayerScreen.razor", "<VisitorIcon Name=\"@GetPoiIcon()\" />", ">@GetPoiIcon()</div>")]
    [InlineData("VisitorMapScreen.razor", "<VisitorIcon Name=\"@CategoryIconSelector(category.Id)\" />", "@CategoryIconSelector(category.Id)</span>")]
    [InlineData("VisitorMapScreen.razor", "<VisitorIcon Name=\"@CategoryIconSelector(State.SelectedPoi.CategoryId)\" />", ">@CategoryIconSelector(State.SelectedPoi.CategoryId)</div>")]
    public void Mobile_formatter_consumers_render_icon_keys_through_visitor_icon(
        string fileName,
        string expectedComponent,
        string forbiddenPlainText)
    {
        var markup = ReadSectionMarkup(fileName);

        Assert.Contains(expectedComponent, markup, StringComparison.Ordinal);
        Assert.DoesNotContain(forbiddenPlainText, markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_category_model_documents_formatter_normalization_of_configured_marker_values()
    {
        var modelPath = Path.Combine(ProjectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorShellModels.cs");
        var source = File.ReadAllText(modelPath);

        Assert.Contains("raw configured values are normalized by", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VisitorCategoryPresentationFormatter", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_shared_icon_uses_a_finite_accessible_vector_glyph_set()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var iconPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Shared", "VisitorIcon.razor");
        var formatterPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Features", "Home", "VisitorCategoryPresentationFormatter.cs");
        var importsPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "_Imports.razor");

        var iconMarkup = File.ReadAllText(iconPath);
        var formatterSource = File.ReadAllText(formatterPath);
        var imports = File.ReadAllText(importsPath);
        var iconNames = new[]
        {
            "discover", "map", "journey", "user", "search", "language", "refresh", "location",
            "audio", "directions", "close", "notification", "history", "download", "settings"
        };

        Assert.Contains("@using NarrationApp.Mobile.Components.Shared", imports, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 24 24\"", iconMarkup, StringComparison.Ordinal);
        Assert.Contains("stroke-width=\"2\"", iconMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-hidden", iconMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-label", iconMarkup, StringComparison.Ordinal);
        Assert.Contains("role=", iconMarkup, StringComparison.Ordinal);
        foreach (var iconName in iconNames)
        {
            Assert.Contains($"\"{iconName}\"", iconMarkup, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("_ =>", iconMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("🦐", iconMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("🍜", formatterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("📍", formatterSource, StringComparison.Ordinal);
        Assert.Contains("\"location\"", formatterSource, StringComparison.Ordinal);
    }

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

        Assert.Contains("case VisitorIntroStep.Welcome:", markup, StringComparison.Ordinal);
        Assert.Contains("case VisitorIntroStep.Language:", markup, StringComparison.Ordinal);
        Assert.Contains("case VisitorIntroStep.Permissions:", markup, StringComparison.Ordinal);
        Assert.Contains("setup-phone-chrome", markup, StringComparison.Ordinal);
        Assert.Contains("setup-statusbar", markup, StringComparison.Ordinal);
        Assert.Contains("setup-dynamic-island", markup, StringComparison.Ordinal);
        Assert.Contains("setup-card--welcome", markup, StringComparison.Ordinal);
        Assert.Contains("setup-stack--welcome", markup, StringComparison.Ordinal);
        Assert.Contains("setup-welcome-hero", markup, StringComparison.Ordinal);
        Assert.Contains("Text.StartExploringButton", markup, StringComparison.Ordinal);
        Assert.Contains("setup-card--language", markup, StringComparison.Ordinal);
        Assert.Contains("setup-stack--language", markup, StringComparison.Ordinal);
        Assert.Contains("setup-card--permissions", markup, StringComparison.Ordinal);
        Assert.Contains("setup-stack--permissions", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-list", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-option__code", markup, StringComparison.Ordinal);
        Assert.Contains("setup-language-option__indicator", markup, StringComparison.Ordinal);
        Assert.Contains("setup-permission-icon", markup, StringComparison.Ordinal);
        Assert.Contains("Text.ContinueButton", markup, StringComparison.Ordinal);
        Assert.Contains("Text.EnableLocationButton", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Bỏ qua — Dùng QR / thủ công", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorAuthScreen", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_screen_component_keeps_fullscreen_overlay_without_in_app_qr_hooks()
    {
        var markup = ReadSectionMarkup("VisitorMapScreen.razor");

        Assert.Contains("map-screen", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-controls", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("map-top-search", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("search-field__hint", markup, StringComparison.Ordinal);
        Assert.Contains("map-category-rail", markup, StringComparison.Ordinal);
        Assert.Contains("notification-panel__surface", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("qr-fab", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("qr-modal", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__grabber", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__audio-status", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__queue", markup, StringComparison.Ordinal);
        Assert.Contains("QueuedPoiStatus", markup, StringComparison.Ordinal);
        Assert.Contains("poi-sheet__directions", markup, StringComparison.Ordinal);
        Assert.Contains("DirectionsStatus", markup, StringComparison.Ordinal);
        Assert.Contains("Text.DirectionsButton", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenDirections", markup, StringComparison.Ordinal);
        Assert.Contains("Text.DetailButton", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenPoiDetail", markup, StringComparison.Ordinal);
        Assert.Contains("geofence-toast geofence-toast--notice", markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("geofence-toast__queue", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_directions_draw_route_in_app_without_auto_launching_external_maps()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var directionsPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.MapDirections.razor.cs");
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var mapPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorMapScreen.razor");

        var source = File.ReadAllText(directionsPath);
        var homeMarkup = File.ReadAllText(homePath);
        var mapMarkup = File.ReadAllText(mapPath);

        Assert.Contains("WalkingDirectionsService.LoadWalkingRouteAsync", source, StringComparison.Ordinal);
        Assert.Contains("_state.OpenPoi(selectedPoi.Id)", source, StringComparison.Ordinal);
        Assert.Contains("GetWalkingDirectionsNotice()", source, StringComparison.Ordinal);
        Assert.Contains("_walkingRoute = result.Route", source, StringComparison.Ordinal);
        Assert.Contains("await RenderMapIfNeededAsync()", source, StringComparison.Ordinal);
        Assert.Contains("DirectionsNotice=\"@GetWalkingDirectionsNotice()\"", homeMarkup, StringComparison.Ordinal);
        Assert.Contains("walking-route-banner", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("DirectionsNotice.DistanceLabel", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("DirectionsNotice.DurationLabel", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("OnClearDirections", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("Text.ClearDirectionsButton", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("OnClearDirections=\"ClearWalkingDirectionsAsync\"", homeMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Launcher.Default.OpenAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VisitorMapDirectionsLinkBuilder", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_keeps_recenter_control_without_zoom_buttons()
    {
        var homeMarkup = File.ReadAllText(Path.Combine(ProjectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor"));
        var mapMarkup = ReadSectionMarkup("VisitorMapScreen.razor");
        var controlsSource = File.ReadAllText(Path.Combine(ProjectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.MapControls.razor.cs"));

        Assert.Contains("OnCenterOnUser=\"CenterMapOnUserAsync\"", homeMarkup, StringComparison.Ordinal);
        Assert.Contains("OnCenterOnUser", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("Text.CenterOnUserLabel", mapMarkup, StringComparison.Ordinal);
        Assert.Contains("visitorMap.centerOnUser", controlsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("OnZoomIn", homeMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("OnZoomOut", homeMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"OnZoomIn\"", mapMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"OnZoomOut\"", mapMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain(">+</button>", mapMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain(">−</button>", mapMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("visitorMap.zoomIn", controlsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("visitorMap.zoomOut", controlsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_poi_detail_exposes_in_app_directions_action()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var homePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Home.razor");
        var detailPath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", "VisitorPoiDetailScreen.razor");

        var homeMarkup = File.ReadAllText(homePath);
        var detailMarkup = File.ReadAllText(detailPath);

        Assert.Contains("OnOpenDirections", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("Text.DirectionsButton", detailMarkup, StringComparison.Ordinal);
        Assert.Contains("OnOpenDirections=\"OpenSelectedPoiDirectionsAsync\"", homeMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_settings_overview_component_keeps_profile_language_and_navigation_hooks()
    {
        var markup = ReadSectionMarkup("VisitorSettingsOverviewScreen.razor");

        Assert.Contains("settings-screen", markup, StringComparison.Ordinal);
        Assert.Contains("settings-profile-card", markup, StringComparison.Ordinal);
        Assert.Contains("settings-stat-grid", markup, StringComparison.Ordinal);
        Assert.Contains("Text.CurrentDeviceLabel", markup, StringComparison.Ordinal);
        Assert.Contains("settings-language-dropdown", markup, StringComparison.Ordinal);
        Assert.Contains("settings-language-select", markup, StringComparison.Ordinal);
        Assert.Contains("OnLanguageChangedAsync", markup, StringComparison.Ordinal);
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
        Assert.Contains("search-field__hint", markup, StringComparison.Ordinal);
        Assert.Contains("OnOpenSearch", markup, StringComparison.Ordinal);
        Assert.Contains("readonly", markup, StringComparison.Ordinal);
        Assert.Contains("theme-chips", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorCityLens", markup, StringComparison.Ordinal);
        Assert.Contains("VisitorStoryCard", markup, StringComparison.Ordinal);
        Assert.Contains("discover-list--stagger", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card--skeleton", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__skeleton", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__media", markup, StringComparison.Ordinal);
        Assert.Contains("discover-poi-card__topline", markup, StringComparison.Ordinal);
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
        Assert.Contains("@Text.PageTitle", aboutMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("NarrationApp Mobile", aboutMarkup, StringComparison.Ordinal);
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
        Assert.Contains("search-field__hint", markup, StringComparison.Ordinal);
        Assert.Contains("search-screen__summary", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-count", markup, StringComparison.Ordinal);
        Assert.Contains("search-section", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item__eyebrow", markup, StringComparison.Ordinal);
        Assert.Contains("search-result-item__summary", markup, StringComparison.Ordinal);
        Assert.Contains("search-suggestion-chips", markup, StringComparison.Ordinal);
        Assert.Contains("<mark>", markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("VisitorStoryCard.razor", "@if (Poi.HasReliableDistance)")]
    [InlineData("VisitorSearchScreen.razor", "@if (poi.HasReliableDistance)")]
    [InlineData("VisitorPoiDetailScreen.razor", "@if (Poi.HasReliableDistance)")]
    public void Direct_poi_distance_markup_requires_reliable_distance(string fileName, string condition)
    {
        var markup = ReadSectionMarkup(fileName);

        Assert.Contains(condition, markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Related_poi_distance_label_requires_reliable_distance()
    {
        var markup = ReadSectionMarkup("VisitorPoiDetailScreen.razor");

        Assert.Contains("poi.HasReliableDistance", markup, StringComparison.Ordinal);
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
        Assert.Contains("full-player-language-picker", markup, StringComparison.Ordinal);
        Assert.Contains("full-player-language-option", markup, StringComparison.Ordinal);
        Assert.Contains("OnToggleLanguagePicker", markup, StringComparison.Ordinal);
        Assert.Contains("OnSelectLanguage.InvokeAsync(language.Code)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"OnCycleLanguage\"", markup, StringComparison.Ordinal);
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
        Assert.DoesNotContain("map-top-search", markup, StringComparison.Ordinal);
        Assert.Contains("map-category-rail", markup, StringComparison.Ordinal);
        Assert.Contains("map-top-overlay--sheet-open", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("placeholder=\"Tìm theo tên, danh mục", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("OnOpenSearch", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("map-overlay-meta", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("LocationStatusLabel", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("DataSourceLabel", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoAudioStatusProvider", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_map_screen_removes_refresh_and_audio_floating_icons()
    {
        var markup = ReadSectionMarkup("VisitorMapScreen.razor");

        Assert.DoesNotContain("map-fab-rail", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("map-fab-button--accent", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("OnSwitchToTours", markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">↻<", markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">🎧<", markup, StringComparison.Ordinal);
    }

    private static string ReadSectionMarkup(string fileName)
    {
        return File.ReadAllText(GetSectionPath(fileName));
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string GetSectionPath(string fileName)
    {
        return Path.Combine(ProjectRoot, "src", "NarrationApp.Mobile", "Components", "Pages", "Sections", fileName);
    }
}
