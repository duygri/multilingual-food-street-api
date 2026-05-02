using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorProximityEngineTests
{
    [Fact]
    public void Evaluate_ReturnsNearestPoiInsideTriggerRadius()
    {
        var location = new VisitorLocationSnapshot(true, true, 10.76095, 106.70545, "Đã định vị");
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 120, "2:44", "Sẵn sàng", 10.7680, 106.7068)
        };

        var match = VisitorProximityEngine.Evaluate(location, pois);

        Assert.NotNull(match);
        Assert.Equal("poi-1", match!.PoiId);
        Assert.True(match.DistanceMeters <= match.TriggerRadiusMeters);
    }

    [Fact]
    public void Evaluate_ReturnsNullWhenNoPoiIsWithinRange()
    {
        var location = new VisitorLocationSnapshot(true, true, 10.7800, 106.7300, "Đã định vị");
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 120, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 120, "2:44", "Sẵn sàng", 10.7680, 106.7068)
        };

        var match = VisitorProximityEngine.Evaluate(location, pois);

        Assert.Null(match);
    }

    [Fact]
    public void Evaluate_PrefersHigherPriorityPoiWhenZonesOverlap()
    {
        var location = new VisitorLocationSnapshot(true, true, 10.76093, 106.70543, "Đã định vị");
        var pois = new[]
        {
            new VisitorPoi("poi-near", "POI gần hơn", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.76092, 106.70542, 4, 1, 120),
            new VisitorPoi("poi-priority", "POI ưu tiên", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 120, "2:44", "Sẵn sàng", 10.76099, 106.70549, 9, 1, 120)
        };

        var match = VisitorProximityEngine.Evaluate(location, pois);

        Assert.NotNull(match);
        Assert.Equal("poi-priority", match!.PoiId);
        Assert.Equal(9, match.Priority);
    }
}
