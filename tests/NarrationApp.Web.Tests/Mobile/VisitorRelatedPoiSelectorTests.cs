using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorRelatedPoiSelectorTests
{
    [Fact]
    public void Select_returns_two_best_matches_prioritizing_category_district_and_distance_gap()
    {
        var selectedPoi = new VisitorPoi(
            "poi-selected",
            "Ốc Oanh",
            "food",
            "Ẩm thực",
            "Khánh Hội",
            "Ngon đêm",
            "Mô tả",
            "Nổi bật",
            10,
            10,
            100,
            "2:10",
            "Ready",
            10.1,
            106.1);

        IReadOnlyList<VisitorPoi> pois =
        [
            selectedPoi,
            new VisitorPoi("poi-1", "Ốc A", "food", "Ẩm thực", "Khánh Hội", "A", "Mô tả", "Nổi bật", 10, 10, 95, "2:10", "Ready", 10.2, 106.2),
            new VisitorPoi("poi-2", "Ốc B", "food", "Ẩm thực", "Quận 4", "B", "Mô tả", "Nổi bật", 10, 10, 110, "2:10", "Ready", 10.3, 106.3),
            new VisitorPoi("poi-3", "Lẩu C", "night", "Đêm", "Khánh Hội", "C", "Mô tả", "Nổi bật", 10, 10, 101, "2:10", "Ready", 10.4, 106.4),
            new VisitorPoi("poi-4", "Bún D", "food", "Ẩm thực", "Khánh Hội", "D", "Mô tả", "Nổi bật", 10, 10, 140, "2:10", "Ready", 10.5, 106.5)
        ];

        var related = VisitorRelatedPoiSelector.Select(pois, selectedPoi);

        Assert.Equal(["poi-1", "poi-4"], related.Select(poi => poi.Id).ToArray());
    }

    [Fact]
    public void Select_returns_empty_when_selected_poi_is_missing()
    {
        var related = VisitorRelatedPoiSelector.Select([], null);

        Assert.Empty(related);
    }
}
