using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorProximityQueueCoordinatorTests
{
    [Fact]
    public void Evaluate_KeepsCurrentActiveAndQueuesTopCandidateWhileNarrationIsLocked()
    {
        var current = new VisitorProximityQueueState(
            ActiveMatch: new VisitorProximityMatch("poi-a", "POI A", 14, 120, 4),
            PendingMatch: null,
            PendingStableSampleCount: 0,
            QueuedMatch: null);

        var candidates = new[]
        {
            new VisitorProximityMatch("poi-b", "POI B", 12, 120, 9),
            new VisitorProximityMatch("poi-a", "POI A", 14, 120, 4)
        };

        var decision = VisitorProximityQueueCoordinator.Evaluate(current, candidates, hasNarrationLock: true);

        Assert.False(decision.ActiveChanged);
        Assert.Equal("poi-a", decision.State.ActiveMatch?.PoiId);
        Assert.Equal("poi-b", decision.State.QueuedMatch?.PoiId);
        Assert.Null(decision.State.PendingMatch);
    }

    [Fact]
    public void Evaluate_PromotesPendingCandidateAfterDebounceThreshold()
    {
        var current = new VisitorProximityQueueState(
            ActiveMatch: new VisitorProximityMatch("poi-a", "POI A", 18, 120, 4),
            PendingMatch: new VisitorProximityMatch("poi-b", "POI B", 12, 120, 9),
            PendingStableSampleCount: 2,
            QueuedMatch: new VisitorProximityMatch("poi-b", "POI B", 12, 120, 9));

        var candidates = new[]
        {
            new VisitorProximityMatch("poi-b", "POI B", 12, 120, 9),
            new VisitorProximityMatch("poi-a", "POI A", 18, 120, 4)
        };

        var decision = VisitorProximityQueueCoordinator.Evaluate(
            current,
            candidates,
            hasNarrationLock: false,
            debounceSampleThreshold: 3);

        Assert.True(decision.ActiveChanged);
        Assert.Equal("poi-b", decision.State.ActiveMatch?.PoiId);
        Assert.Null(decision.State.PendingMatch);
    }
}
