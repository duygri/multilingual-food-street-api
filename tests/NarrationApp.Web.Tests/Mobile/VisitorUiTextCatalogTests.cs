using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorUiTextCatalogTests
{
    [Fact]
    public void ForLanguage_returns_english_ui_copy_for_english_selection()
    {
        var text = VisitorUiTextCatalog.ForLanguage("en");

        Assert.Equal("Welcome to", text.WelcomeTitlePrefix);
        Assert.Equal("Start exploring ->", text.StartExploringButton);
        Assert.Equal("Map", text.TabMap);
        Assert.Equal("Discover", text.TabDiscover);
        Assert.Equal("Settings", text.HeaderSettingsTitle);
        Assert.Equal("Choose app language", text.SettingsLanguageSelectLabel);
        Assert.Equal("Listen now", text.ListenNowButton);
        Assert.Equal("Narration language", text.NarrationLanguageLabel());
        Assert.Equal("Audio playback failed", text.AudioPlaybackFailedLabel());
        Assert.Equal("Ready to play • Vietnamese • recorded", text.LocalizeKnownStatus("Sẵn sàng phát • Tiếng Việt • ghi âm"));
        Assert.Equal("Sai Gon Ke", text.PageTitle);
        Assert.Equal("Hear the streets, taste Saigon.", text.BrandTagline());
        Assert.Equal("Discover", text.TabDiscover);
        Assert.Equal("Map", text.TabMap);
        Assert.Equal("Journeys", text.TabTours);
        Assert.Equal("My", text.TabMe);
    }

    [Fact]
    public void ForLanguage_keeps_non_app_languages_on_vietnamese_ui_copy()
    {
        var text = VisitorUiTextCatalog.ForLanguage("ja");

        Assert.Equal("Chào mừng đến", text.WelcomeTitlePrefix);
        Assert.Equal("Bắt đầu khám phá ->", text.StartExploringButton);
        Assert.Equal("Bản đồ", text.TabMap);
        Assert.Equal("Khám phá", text.TabDiscover);
        Assert.Equal("Cài đặt", text.HeaderSettingsTitle);
        Assert.Equal("Sài Gòn Kể", text.PageTitle);
        Assert.Equal("Nghe chuyện phố, nếm vị Sài Gòn.", text.BrandTagline());
        Assert.Equal("Khám phá", text.TabDiscover);
        Assert.Equal("Bản đồ", text.TabMap);
        Assert.Equal("Hành trình", text.TabTours);
        Assert.Equal("Của tôi", text.TabMe);
    }

    [Fact]
    public void IsSupportedAppLanguage_only_allows_vietnamese_and_english()
    {
        Assert.True(VisitorUiTextCatalog.IsSupportedAppLanguage("vi"));
        Assert.True(VisitorUiTextCatalog.IsSupportedAppLanguage("en"));
        Assert.False(VisitorUiTextCatalog.IsSupportedAppLanguage("ja"));
        Assert.False(VisitorUiTextCatalog.IsSupportedAppLanguage("fr"));
    }

    [Fact]
    public void ForLanguage_falls_back_to_vietnamese_for_unknown_language()
    {
        var text = VisitorUiTextCatalog.ForLanguage("xx");

        Assert.Equal("Chào mừng đến", text.WelcomeTitlePrefix);
        Assert.Equal("Bắt đầu khám phá ->", text.StartExploringButton);
        Assert.Equal("Bản đồ", text.TabMap);
    }

    [Fact]
    public void Visitor_brand_localizes_native_titles_and_background_tracking_copy()
    {
        Assert.Equal("visitor.preferences.app-language-code", VisitorBrand.PreferredAppLanguageCodeKey);
        Assert.Equal("Sài Gòn Kể", VisitorBrand.DisplayName("vi"));
        Assert.Equal("Sai Gon Ke", VisitorBrand.DisplayName("en-US"));

        var vietnamese = VisitorBrand.BackgroundTrackingNotification("vi", 12);
        Assert.Equal("Sài Gòn Kể", vietnamese.Title);
        Assert.Equal("Đang theo dõi vị trí trong nền • chu kỳ 12 giây", vietnamese.Body);

        var english = VisitorBrand.BackgroundTrackingNotification("en", 12);
        Assert.Equal("Sai Gon Ke", english.Title);
        Assert.Equal("Background location tracking is active • every 12 seconds", english.Body);
    }

    [Fact]
    public void Navigation_accessibility_copy_and_current_state_are_localized()
    {
        Assert.Equal("Điều hướng chính", VisitorUiTextCatalog.ForLanguage("vi").BottomNavigationLabel());
        Assert.Equal("Primary navigation", VisitorUiTextCatalog.ForLanguage("en").BottomNavigationLabel());
        Assert.Equal("page", VisitorNavigationPresentationFormatter.GetAriaCurrent(isActive: true));
        Assert.Null(VisitorNavigationPresentationFormatter.GetAriaCurrent(isActive: false));

        var tabs = new[] { VisitorTab.Discover, VisitorTab.Map, VisitorTab.Tours, VisitorTab.Settings };
        foreach (var activeTab in tabs)
        {
            var currentItems = tabs.Count(tab =>
                VisitorNavigationPresentationFormatter.GetAriaCurrent(tab == activeTab) == "page");
            Assert.Equal(1, currentItems);
        }
    }
}
