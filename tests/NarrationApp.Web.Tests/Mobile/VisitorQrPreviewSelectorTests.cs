using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorQrPreviewSelectorTests
{
    [Fact]
    public void Preview_poi_prefers_selected_then_featured_then_full_list()
    {
        var poiA = CreatePoi("poi-a");
        var poiB = CreatePoi("poi-b");
        var poiC = CreatePoi("poi-c");

        Assert.Equal("poi-a", VisitorQrPreviewSelector.GetPreviewPoi(poiA, [poiB], [poiC])!.Id);
        Assert.Equal("poi-b", VisitorQrPreviewSelector.GetPreviewPoi(null, [poiB], [poiC])!.Id);
        Assert.Equal("poi-c", VisitorQrPreviewSelector.GetPreviewPoi(null, [], [poiC])!.Id);
    }

    [Fact]
    public void Preview_code_uses_poi_then_open_app_fallbacks()
    {
        Assert.Equal("A", VisitorQrPreviewSelector.GetPreviewCode(CreatePoi("poi-a")));
        Assert.Equal("OPEN-APP", VisitorQrPreviewSelector.GetPreviewCode(null));
    }

    private static VisitorPoi CreatePoi(string id) =>
        new(
            id,
            "POI",
            "food",
            "Ẩm thực",
            "Khánh Hội",
            "Tag",
            "Mô tả",
            "Nổi bật",
            10,
            10,
            100,
            "2:10",
            "Ready",
            10.1,
            106.1);
}
