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
}
