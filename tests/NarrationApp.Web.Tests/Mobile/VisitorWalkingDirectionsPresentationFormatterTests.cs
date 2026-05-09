using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorWalkingDirectionsPresentationFormatterTests
{
    [Fact]
    public void Build_ReturnsReadableDistanceAndDurationForActiveRoute()
    {
        var poi = CreatePoi("poi-oc-oanh", "Ốc Oanh");
        var route = new VisitorMapRoute(
            [new VisitorMapRoutePoint(10.7600, 106.7040), new VisitorMapRoutePoint(10.7620, 106.7060)],
            "Đi bộ 1.4 km • khoảng 1 giờ 12 phút",
            DistanceMeters: 1420,
            DurationMinutes: 72);

        var notice = VisitorWalkingDirectionsPresentationFormatter.Build(
            poi,
            routePoiId: "poi-oc-oanh",
            route,
            statusLabel: route.StatusLabel,
            isLoading: false);

        Assert.NotNull(notice);
        Assert.Equal("Dẫn tới Ốc Oanh", notice!.Title);
        Assert.Equal("1.4 km", notice.DistanceLabel);
        Assert.Equal("1 giờ 12 phút", notice.DurationLabel);
        Assert.Equal("Đi bộ 1.4 km • khoảng 1 giờ 12 phút", notice.StatusLabel);
        Assert.True(notice.IsAvailable);
        Assert.False(notice.IsLoading);
    }

    [Fact]
    public void Build_ReturnsLoadingNoticeBeforeRouteIsAvailable()
    {
        var poi = CreatePoi("poi-oc-oanh", "Ốc Oanh");

        var notice = VisitorWalkingDirectionsPresentationFormatter.Build(
            poi,
            routePoiId: "poi-oc-oanh",
            route: null,
            statusLabel: null,
            isLoading: true);

        Assert.NotNull(notice);
        Assert.Equal("Đang tìm đường tới Ốc Oanh", notice!.Title);
        Assert.Equal("Đang vẽ đường đi bộ trong app...", notice.StatusLabel);
        Assert.Null(notice.DistanceLabel);
        Assert.Null(notice.DurationLabel);
        Assert.True(notice.IsLoading);
        Assert.False(notice.IsAvailable);
    }

    private static VisitorPoi CreatePoi(string id, string name) =>
        new(
            id,
            name,
            "hai-san",
            "Hải sản",
            "Quận 4",
            "Live API",
            "Mô tả",
            "Nổi bật",
            30,
            40,
            120,
            "1:10",
            "Sẵn sàng",
            10.7600,
            106.7040);
}
