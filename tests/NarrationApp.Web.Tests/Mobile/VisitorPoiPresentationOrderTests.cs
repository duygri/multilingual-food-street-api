using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorPoiPresentationOrderTests
{
    [Fact]
    public void Apply_WithValidGps_OrdersByPriorityThenDistanceThenOrdinalId()
    {
        VisitorPoi[] source =
        [
            CreatePoi("poi-low", priority: 2, distanceMeters: 1),
            CreatePoi("poi-b", priority: 5, distanceMeters: 300),
            CreatePoi("poi-a", priority: 5, distanceMeters: 300),
            CreatePoi("poi-near", priority: 5, distanceMeters: 100)
        ];

        var ordered = VisitorPoiPresentationOrder.Apply(source, hasValidGps: true);

        Assert.Equal(["poi-near", "poi-a", "poi-b", "poi-low"], ordered.Select(poi => poi.Id));
        Assert.NotSame(source, ordered);
        Assert.Equal(["poi-low", "poi-b", "poi-a", "poi-near"], source.Select(poi => poi.Id));
    }

    [Fact]
    public void Apply_WithoutValidGps_OrdersByPriorityThenOrdinalId()
    {
        VisitorPoi[] source =
        [
            CreatePoi("poi-z", priority: 3, distanceMeters: 1),
            CreatePoi("poi-B", priority: 7, distanceMeters: 900),
            CreatePoi("poi-a", priority: 7, distanceMeters: 2),
            CreatePoi("poi-A", priority: 7, distanceMeters: 500)
        ];

        var ordered = VisitorPoiPresentationOrder.Apply(source, hasValidGps: false);

        Assert.Equal(["poi-A", "poi-B", "poi-a", "poi-z"], ordered.Select(poi => poi.Id));
        Assert.NotSame(source, ordered);
    }

    private static VisitorPoi CreatePoi(string id, int priority, int distanceMeters) =>
        new(
            id,
            id,
            "history",
            "Lịch sử",
            "TP.HCM",
            "Test",
            "Test POI",
            "Test",
            50,
            50,
            distanceMeters,
            "1:00",
            "Sẵn sàng",
            10.7769,
            106.7009,
            Priority: priority);
}
