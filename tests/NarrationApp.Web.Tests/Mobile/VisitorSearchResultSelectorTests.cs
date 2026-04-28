using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorSearchResultSelectorTests
{
    private static readonly IReadOnlyList<VisitorPoi> Pois =
    [
        new VisitorPoi(
            "poi-1",
            "Bún cá Châu Đốc",
            "food",
            "Ẩm thực",
            "Ẩm thực",
            "Ready",
            "Bún cá nóng",
            "Nổi bật",
            20,
            30,
            120,
            "2:10",
            "Ready",
            10.1,
            106.1),
        new VisitorPoi(
            "poi-2",
            "Cầu Khánh Hội",
            "river",
            "Ven sông",
            "Ven sông",
            "Ready",
            "Nhìn ra sông",
            "Nổi bật",
            40,
            30,
            120,
            "2:10",
            "Ready",
            10.2,
            106.2),
        new VisitorPoi(
            "poi-3",
            "Lẩu đêm Vĩnh Khánh",
            "night",
            "Đêm",
            "Đêm",
            "Ready",
            "Ăn đêm",
            "Nổi bật",
            60,
            30,
            120,
            "2:10",
            "Ready",
            10.3,
            106.3)
    ];

    private static readonly IReadOnlyList<VisitorTourCard> Tours =
    [
        new VisitorTourCard("tour-1", "Tour ẩm thực", "3 điểm dừng", "30 phút", "Dễ", "Đi qua món ngon", ["poi-1", "poi-3"]),
        new VisitorTourCard("tour-2", "Tour ven sông", "2 điểm dừng", "20 phút", "Dễ", "Đi bộ ven sông", ["poi-2"])
    ];

    [Fact]
    public void Poi_results_limit_changes_with_search_term()
    {
        var manyPois = Enumerable.Range(1, 10)
            .Select(index => new VisitorPoi(
                $"poi-{index}",
                $"POI {index}",
                "food",
                "Ẩm thực",
                "Ẩm thực",
                "Ready",
                "Mô tả",
                "Nổi bật",
                index,
                30,
                120,
                "2:10",
                "Ready",
                10.0 + index,
                106.0 + index))
            .ToArray();

        var withoutSearch = VisitorSearchResultSelector.GetPoiResults(manyPois, string.Empty);
        var withSearch = VisitorSearchResultSelector.GetPoiResults(manyPois, "poi");

        Assert.Equal(6, withoutSearch.Count);
        Assert.Equal(8, withSearch.Count);
        Assert.True(withoutSearch.SequenceEqual(withoutSearch.OrderBy(poi => poi.DistanceMeters)));
    }

    [Fact]
    public void Tour_results_filter_by_selected_category()
    {
        var results = VisitorSearchResultSelector.GetTourResults(Tours, Pois, "river", string.Empty);

        var tour = Assert.Single(results);
        Assert.Equal("tour-2", tour.Id);
    }

    [Fact]
    public void Tour_results_match_accent_insensitive_search_on_title_description_and_stop_names()
    {
        var titleMatch = VisitorSearchResultSelector.GetTourResults(Tours, Pois, "all", "am thuc");
        var stopNameMatch = VisitorSearchResultSelector.GetTourResults(Tours, Pois, "all", "chau doc");

        Assert.Equal("tour-1", Assert.Single(titleMatch).Id);
        Assert.Equal("tour-1", Assert.Single(stopNameMatch).Id);
    }

    [Fact]
    public void Result_count_combines_poi_and_tour_results()
    {
        var poiResults = VisitorSearchResultSelector.GetPoiResults(Pois, string.Empty);
        var tourResults = VisitorSearchResultSelector.GetTourResults(Tours, Pois, "all", string.Empty);

        var total = VisitorSearchResultSelector.GetResultCount(poiResults, tourResults);

        Assert.Equal(poiResults.Count + tourResults.Count, total);
    }
}
