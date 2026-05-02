using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapQueueStatusFormatterTests
{
    [Fact]
    public void Build_ReturnsNullWhenNoQueuedPoiExists()
    {
        var result = VisitorMapQueueStatusFormatter.Build(
            selectedPoiId: "poi-a",
            activeProximity: new VisitorProximityMatch("poi-a", "POI A", 14, 120, 4),
            queuedMatch: null);

        Assert.Null(result);
    }

    [Fact]
    public void Build_DescribesQueuedPoiWhenOverlapHasNextCandidate()
    {
        var result = VisitorMapQueueStatusFormatter.Build(
            selectedPoiId: "poi-a",
            activeProximity: new VisitorProximityMatch("poi-a", "POI A", 14, 120, 4),
            queuedMatch: new VisitorProximityMatch("poi-b", "POI B", 18, 120, 9));

        Assert.Equal("Tiếp theo nếu còn đứng trong vùng: POI B • ưu tiên 9", result);
    }
}
