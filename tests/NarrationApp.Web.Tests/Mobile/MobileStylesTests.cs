using System.IO;
using System.Text;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class MobileStylesTests
{
    [Fact]
    public void Mobile_styles_are_split_into_domain_modules()
    {
        var webRoot = GetMobileWebRoot();
        var appStylePath = Path.Combine(webRoot, "app.css");
        var entryCss = File.ReadAllText(appStylePath);
        var moduleNames = new[]
        {
            "mobile-foundation.css",
            "mobile-map.css",
            "mobile-discovery.css",
            "mobile-tours.css",
            "mobile-settings.css",
            "mobile-shell.css"
        };

        foreach (var moduleName in moduleNames)
        {
            Assert.Contains($"@import url(\"css/{moduleName}\");", entryCss, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(webRoot, "css", moduleName)), $"{moduleName} should exist under wwwroot/css.");
        }

        Assert.True(entryCss.Split('\n').Length <= 16, "app.css should stay as the mobile stylesheet entry point, not the full stylesheet.");
        Assert.DoesNotContain(".map-screen", entryCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".settings-screen", entryCss, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_styles_define_bottom_safe_offsets_for_fixed_navigation()
    {
        var css = ReadMobileCss();

        Assert.Contains("--mobile-bottom-nav-height", css, StringComparison.Ordinal);
        Assert.Contains("--mobile-mini-player-height", css, StringComparison.Ordinal);
        Assert.Contains("--mobile-bottom-safe-offset", css, StringComparison.Ordinal);
        Assert.Contains("padding: max(12px, env(safe-area-inset-top)) 12px calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom));", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_setup_flow_stays_top_aligned_with_balanced_language_cards()
    {
        var css = ReadMobileCss();

        Assert.Contains(".visitor-app--setup", css, StringComparison.Ordinal);
        Assert.Contains("justify-content: flex-start;", css, StringComparison.Ordinal);
        Assert.Contains(".setup-card--language", css, StringComparison.Ordinal);
        Assert.Contains(".setup-phone-chrome", css, StringComparison.Ordinal);
        Assert.Contains(".setup-statusbar", css, StringComparison.Ordinal);
        Assert.Contains(".setup-dynamic-island", css, StringComparison.Ordinal);
        Assert.Contains(".setup-stack--language", css, StringComparison.Ordinal);
        Assert.Contains(".setup-language-list", css, StringComparison.Ordinal);
        Assert.Contains(".setup-language-option", css, StringComparison.Ordinal);
        Assert.Contains(".setup-language-option__code", css, StringComparison.Ordinal);
        Assert.Contains(".setup-language-option__indicator", css, StringComparison.Ordinal);
        Assert.Contains(".setup-actions--stacked", css, StringComparison.Ordinal);
        Assert.Contains(".setup-permission-icon", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_setup_flow_uses_compact_phone_scale_for_intro_steps()
    {
        var css = ReadMobileCss();

        Assert.Contains("width: min(100%, 392px);", css, StringComparison.Ordinal);
        Assert.Contains("padding: 14px 18px 20px;", css, StringComparison.Ordinal);
        Assert.Contains("gap: 16px;", css, StringComparison.Ordinal);
        Assert.Contains("gap: 18px;", css, StringComparison.Ordinal);
        Assert.Contains("font-size: clamp(1.72rem, 7.2vw, 2.08rem);", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 0.96rem;", css, StringComparison.Ordinal);
        Assert.Contains("margin-bottom: 18px;", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 34px;", css, StringComparison.Ordinal);
        Assert.Contains("width: 104px;", css, StringComparison.Ordinal);
        Assert.Contains("height: 30px;", css, StringComparison.Ordinal);
        Assert.Contains("width: 72px;", css, StringComparison.Ordinal);
        Assert.Contains("height: 72px;", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 74px;", css, StringComparison.Ordinal);
        Assert.Contains("padding: 13px 15px;", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 1.08rem;", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 0.9rem;", css, StringComparison.Ordinal);
        Assert.Contains("width: 24px;", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 50px;", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 1rem;", css, StringComparison.Ordinal);
        Assert.Contains("max-width: 320px;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_styles_render_map_as_fullscreen_shell_with_floating_poi_sheet()
    {
        var css = ReadMobileCss();

        Assert.Contains(".map-screen", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-overlay", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-controls", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-search", css, StringComparison.Ordinal);
        Assert.Contains(".map-category-rail", css, StringComparison.Ordinal);
        Assert.Contains(".map-shell::before", css, StringComparison.Ordinal);
        Assert.Contains(".category-strip--map .category-chip", css, StringComparison.Ordinal);
        Assert.Contains(".map-icon-button,", css, StringComparison.Ordinal);
        Assert.Contains(".map-top-overlay--sheet-open .map-category-rail", css, StringComparison.Ordinal);
        Assert.Contains(".map-zoom-controls", css, StringComparison.Ordinal);
        Assert.Contains(".map-pill", css, StringComparison.Ordinal);
        Assert.Contains(".map-shell", css, StringComparison.Ordinal);
        Assert.Contains(".poi-sheet--floating", css, StringComparison.Ordinal);
        Assert.Contains("max-height: calc(100% - 168px);", css, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--mobile-bottom-nav-height) + env(safe-area-inset-bottom) + 12px);", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".qr-fab", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".qr-modal", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".qr-target-btn", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_styles_include_sample_strict_discover_poi_and_tour_sections()
    {
        var css = ReadMobileCss();

        Assert.Contains(".discover-screen", css, StringComparison.Ordinal);
        Assert.Contains(".discover-header__title", css, StringComparison.Ordinal);
        Assert.Contains(".discover-header__icon", css, StringComparison.Ordinal);
        Assert.Contains(".discover-refresh-button", css, StringComparison.Ordinal);
        Assert.Contains(".discover-search--strict", css, StringComparison.Ordinal);
        Assert.Contains(".discover-chip-row", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card--skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__media", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__image", css, StringComparison.Ordinal);
        Assert.Contains(".discover-poi-card__image-fallback", css, StringComparison.Ordinal);
        Assert.Contains(".discover-list--stagger", css, StringComparison.Ordinal);
        Assert.Contains(".discover-sync-banner", css, StringComparison.Ordinal);
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
        Assert.Contains(".discover-poi-card__image-fallback.is-drink", css, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom) + 72px);", css, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--mobile-bottom-safe-offset) + env(safe-area-inset-bottom) + 16px);", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-screen", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-header__copy", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__cover", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__badge", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".tour-guest-prompt", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card--skeleton", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-list--stagger", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__summary", css, StringComparison.Ordinal);
        Assert.Contains(".tour-showcase-card__description", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-footer-stats", css, StringComparison.Ordinal);
        Assert.Contains(".tour-list-footer-stats__item", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-screen", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-screen__intro", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-screen__panel", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-feature-list", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-feature-card__copy", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-screen__guest", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".guest-auth-snackbar", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".guest-auth-snackbar__copy", css, StringComparison.Ordinal);
        Assert.Contains(".settings-profile-card", css, StringComparison.Ordinal);
        Assert.Contains(".settings-profile-card__top", css, StringComparison.Ordinal);
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
        Assert.Contains(".full-player-topbar", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-hero", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-summary__meta", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-progress", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-wave", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-controls", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-control--primary", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-transcript", css, StringComparison.Ordinal);
        Assert.Contains(".full-player-transcript__surface", css, StringComparison.Ordinal);
        Assert.Contains(".notification-panel__surface", css, StringComparison.Ordinal);
        Assert.Contains(".notification-panel__copy", css, StringComparison.Ordinal);
        Assert.Contains(".mini-player", css, StringComparison.Ordinal);
        Assert.Contains(".bottom-nav", css, StringComparison.Ordinal);
        Assert.Contains(".nav-pill", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".qr-modal__panel", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".auth-overlay__card", css, StringComparison.Ordinal);
        Assert.Contains(".offline-banner", css, StringComparison.Ordinal);
        Assert.Contains(".geofence-toast", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-screen-enter", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-spring-pop", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-shimmer-sweep", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes mobile-hero-float", css, StringComparison.Ordinal);
        Assert.Contains(".nav-pill.is-active::before", css, StringComparison.Ordinal);
    }

    private static string ReadMobileCss()
    {
        var webRoot = GetMobileWebRoot();
        var entryCss = File.ReadAllText(Path.Combine(webRoot, "app.css"));
        var builder = new StringBuilder(entryCss);

        foreach (var line in entryCss.Split('\n'))
        {
            var trimmed = line.Trim();
            const string prefix = "@import url(\"";
            const string suffix = "\");";
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal) || !trimmed.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePath = trimmed[prefix.Length..^suffix.Length].Replace('/', Path.DirectorySeparatorChar);
            var importedPath = Path.Combine(webRoot, relativePath);
            if (File.Exists(importedPath))
            {
                builder.AppendLine();
                builder.AppendLine(File.ReadAllText(importedPath));
            }
        }

        return builder.ToString();
    }

    private static string GetMobileWebRoot()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(projectRoot, "src", "NarrationApp.Mobile", "wwwroot");
    }
}
