using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorProximityDebugEventFormatterTests
{
    [Fact]
    public void BuildActiveEvent_IncludesPoiNameDistanceAndPriority()
    {
        var message = VisitorProximityDebugEventFormatter.BuildActive(
            new VisitorProximityMatch("poi-a", "POI A", 14, 120, 4));

        Assert.Equal("Active • POI A • 14m • ưu tiên 4", message);
    }

    [Fact]
    public void BuildQueuedAndPromotedEvents_AreDistinct()
    {
        var match = new VisitorProximityMatch("poi-b", "POI B", 18, 120, 9);

        Assert.Equal("Queued • POI B • 18m • ưu tiên 9", VisitorProximityDebugEventFormatter.BuildQueued(match));
        Assert.Equal("Promoted • POI B • 18m • ưu tiên 9", VisitorProximityDebugEventFormatter.BuildPromoted(match));
        Assert.Equal("Exited • POI B", VisitorProximityDebugEventFormatter.BuildExited(match));
    }
}
