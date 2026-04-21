using System.IO;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileStylesTests
{
    [Fact]
    public void Mobile_styles_define_bottom_safe_offsets_for_fixed_navigation()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var stylePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "app.css");

        var css = File.ReadAllText(stylePath);

        Assert.Contains("--mobile-bottom-nav-height", css, StringComparison.Ordinal);
        Assert.Contains("--mobile-mini-player-height", css, StringComparison.Ordinal);
        Assert.Contains("--mobile-bottom-safe-offset", css, StringComparison.Ordinal);
        Assert.Contains("padding: max(12px, env(safe-area-inset-top)) 12px calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom));", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_styles_render_map_as_fullscreen_shell_with_floating_poi_sheet()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var stylePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "app.css");

        var css = File.ReadAllText(stylePath);

        Assert.Contains(".map-screen", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-overlay", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-controls", css, StringComparison.Ordinal);
        Assert.Contains(".map-category-rail", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-overlay--sheet-open .map-category-rail", css, StringComparison.Ordinal);
        Assert.Contains(".map-shell", css, StringComparison.Ordinal);
        Assert.Contains(".poi-sheet--floating", css, StringComparison.Ordinal);
        Assert.Contains("max-height: calc(100% - 168px);", css, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--mobile-bottom-nav-height) + env(safe-area-inset-bottom) + 12px);", css, StringComparison.Ordinal);
        Assert.Contains(".qr-fab", css, StringComparison.Ordinal);
        Assert.Contains(".qr-modal", css, StringComparison.Ordinal);
        Assert.Contains(".qr-target-btn", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_styles_include_sample_strict_discover_poi_and_tour_sections()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var stylePath = Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot", "app.css");

        var css = File.ReadAllText(stylePath);

        Assert.Contains(".discover-screen", css, StringComparison.Ordinal);
        Assert.Contains(".discover-refresh-button", css, StringComparison.Ordinal);
        Assert.Contains(".discover-search--strict", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card--skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".discover-list--stagger", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__topline", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__title", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__summary", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__status", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-screen", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-headline__eyebrow", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-headline__chips", css, StringComparison.Ordinal);
        Assert.Contains(".poi-audio-panel", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-audio-card", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-audio-card__wave", css, StringComparison.Ordinal);
        Assert.Contains(".poi-audio-panel__player", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-audio-heading", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-audio-heading__title", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-audio-heading__action", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-headline__distance", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-copy", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-transcript", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-related", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-related__header", css, StringComparison.Ordinal);
        Assert.Contains(".poi-detail-sticky-cta", css, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom) + 72px);", css, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom) + 16px);", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-screen", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-header__copy", css, StringComparison.Ordinal);
        Assert.Contains(".tour-guest-prompt", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card--skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-list--stagger", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__summary", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__description", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-footer-stats", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-footer-stats__item", css, StringComparison.Ordinal);
        Assert.Contains(".auth-screen", css, StringComparison.Ordinal);
        Assert.Contains(".auth-screen__intro", css, StringComparison.Ordinal);
        Assert.Contains(".auth-screen__panel", css, StringComparison.Ordinal);
        Assert.Contains(".auth-feature-list", css, StringComparison.Ordinal);
        Assert.Contains(".auth-feature-card__copy", css, StringComparison.Ordinal);
        Assert.Contains(".auth-screen__guest", css, StringComparison.Ordinal);
        Assert.Contains(".guest-auth-snackbar", css, StringComparison.Ordinal);
        Assert.Contains(".guest-auth-snackbar__copy", css, StringComparison.Ordinal);
        Assert.Contains(".settings-profile-card", css, StringComparison.Ordinal);
        Assert.Contains(".settings-avatar", css, StringComparison.Ordinal);
        Assert.Contains(".settings-stat-grid", css, StringComparison.Ordinal);
        Assert.Contains(".settings-nav-list", css, StringComparison.Ordinal);
        Assert.Contains(".settings-row", css, StringComparison.Ordinal);
        Assert.Contains(".settings-language-strip", css, StringComparison.Ordinal);
        Assert.Contains(".settings-detail-screen", css, StringComparison.Ordinal);
        Assert.Contains(".settings-detail-header", css, StringComparison.Ordinal);
        Assert.Contains(".settings-toggle-row", css, StringComparison.Ordinal);
        Assert.Contains(".settings-segment", css, StringComparison.Ordinal);
        Assert.Contains(".settings-speed-grid", css, StringComparison.Ordinal);
        Assert.Contains(".cache-summary-card", css, StringComparison.Ordinal);
        Assert.Contains(".cache-item", css, StringComparison.Ordinal);
        Assert.Contains(".history-day", css, StringComparison.Ordinal);
        Assert.Contains(".history-entry", css, StringComparison.Ordinal);
        Assert.Contains(".about-info-card", css, StringComparison.Ordinal);
        Assert.Contains(".about-link-list", css, StringComparison.Ordinal);
        Assert.Contains(".profile-editor-card", css, StringComparison.Ordinal);
        Assert.Contains(".profile-editor-stats", css, StringComparison.Ordinal);
        Assert.Contains(".tour-detail-screen", css, StringComparison.Ordinal);
        Assert.Contains(".tour-detail-hero__pill", css, StringComparison.Ordinal);
        Assert.Contains(".tour-detail-hero__copy", css, StringComparison.Ordinal);
        Assert.Contains(".tour-detail-overview", css, StringComparison.Ordinal);
        Assert.Contains(".tour-detail-sheet-actions", css, StringComparison.Ordinal);
        Assert.Contains(".tour-stop-timeline", css, StringComparison.Ordinal);
        Assert.Contains(".tour-progress-track__summary", css, StringComparison.Ordinal);
        Assert.Contains(".tour-stop-timeline__copy", css, StringComparison.Ordinal);
        Assert.Contains(".search-screen", css, StringComparison.Ordinal);
        Assert.Contains(".search-screen__summary", css, StringComparison.Ordinal);
        Assert.Contains(".search-top__field--strict", css, StringComparison.Ordinal);
        Assert.Contains(".search-result-item", css, StringComparison.Ordinal);
        Assert.Contains(".search-result-item__eyebrow", css, StringComparison.Ordinal);
        Assert.Contains(".search-result-item__summary", css, StringComparison.Ordinal);
        Assert.Contains(".search-suggestion-chips", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-screen", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-summary__meta", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-progress", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-wave", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-controls", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-control--primary", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-transcript", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-transcript__surface", css, StringComparison.Ordinal);
        Assert.Contains(".notification-panel__surface", css, StringComparison.Ordinal);
        Assert.Contains(".notification-panel__copy", css, StringComparison.Ordinal);
        Assert.Contains(".qr-modal__panel", css, StringComparison.Ordinal);
        Assert.Contains(".auth-overlay__card", css, StringComparison.Ordinal);
        Assert.Contains(".offline-banner", css, StringComparison.Ordinal);
        Assert.Contains(".geofence-toast", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-screen-enter", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-spring-pop", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-shimmer-sweep", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-hero-float", css, StringComparison.Ordinal);
        Assert.Contains(".nav-pill.is-active::before", css, StringComparison.Ordinal);
    }
}
