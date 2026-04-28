using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorNavigationPresentationFormatterTests
{
    [Fact]
    public void Header_and_shell_classes_follow_current_tab()
    {
        Assert.Equal("Bản đồ", VisitorNavigationPresentationFormatter.GetHeaderTitle(VisitorTab.Map));
        Assert.Equal("Khám phá", VisitorNavigationPresentationFormatter.GetHeaderTitle(VisitorTab.Discover));
        Assert.Equal("visitor-app--map", VisitorNavigationPresentationFormatter.GetVisitorAppClass(VisitorTab.Map));
        Assert.Equal(string.Empty, VisitorNavigationPresentationFormatter.GetVisitorAppClass(VisitorTab.Settings));
        Assert.Equal("visitor-frame--map", VisitorNavigationPresentationFormatter.GetVisitorFrameClass(VisitorTab.Map));
    }

    [Fact]
    public void Selection_helpers_and_category_icon_are_stable()
    {
        Assert.Equal("is-selected", VisitorNavigationPresentationFormatter.GetSelectionClass(isSelected: true));
        Assert.Equal(string.Empty, VisitorNavigationPresentationFormatter.GetSelectionClass(isSelected: false));
        Assert.Equal("🍜", VisitorNavigationPresentationFormatter.GetCategoryIcon("food"));
        Assert.Equal("☕", VisitorNavigationPresentationFormatter.GetCategoryIcon("other"));
    }

    [Fact]
    public void Poi_category_label_and_auto_audio_messages_use_fallbacks()
    {
        var poi = new VisitorPoi(
            "poi-1",
            "Ốc Oanh",
            "food",
            "",
            "Khánh Hội",
            "Ngon",
            "Mô tả",
            "Nổi bật",
            10,
            10,
            100,
            "2:10",
            "Ready",
            10.1,
            106.1);
        IReadOnlyList<VisitorCategory> categories =
        [
            new VisitorCategory("food", "Ẩm thực", "•")
        ];
        var proximity = new VisitorProximityMatch("poi-1", "Ốc Oanh", 24, 30);

        Assert.Equal("Ẩm thực", VisitorNavigationPresentationFormatter.GetPoiCategoryLabel(poi, categories));
        Assert.Equal("Đã vào vùng Ốc Oanh", VisitorNavigationPresentationFormatter.GetAutoAudioStatus(true, proximity));
        Assert.Equal("Audio tự động đang tắt", VisitorNavigationPresentationFormatter.GetAutoAudioStatus(false, proximity));
        Assert.Equal("Đang phát thuyết minh tự động • 24m", VisitorNavigationPresentationFormatter.GetGeofenceToastMessage(true, proximity, true));
        Assert.Equal("Bạn đã tắt auto-play. Chạm vào POI để nghe thủ công.", VisitorNavigationPresentationFormatter.GetGeofenceToastMessage(false, proximity, false));
    }
}
