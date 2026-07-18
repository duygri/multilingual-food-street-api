using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorPoiDistanceProjectorTests
{
    [Fact]
    public void Apply_WithValidLocation_ProjectsFreshReliableDistances()
    {
        var source = new[] { CreatePoi(777) };

        var projected = VisitorPoiDistanceProjector.Apply(
            source,
            new VisitorLocationSnapshot(true, true, 10.7607, 106.7033, "GPS live"));

        var poi = Assert.Single(projected);
        Assert.Equal(0, poi.DistanceMeters);
        Assert.True(poi.HasReliableDistance);
        Assert.NotSame(source, projected);
    }

    [Fact]
    public void Apply_WithoutValidLocation_ClearsStaleDistanceAndReliability()
    {
        var source = new[] { CreatePoi(777) with { HasReliableDistance = true } };

        var projected = VisitorPoiDistanceProjector.Apply(source, VisitorLocationSnapshot.Disabled());

        var poi = Assert.Single(projected);
        Assert.Equal(0, poi.DistanceMeters);
        Assert.False(poi.HasReliableDistance);
        Assert.NotSame(source, projected);
    }

    [Fact]
    public void Apply_WithoutValidLocation_ReturnsSameListWhenDistancesAreAlreadyUnavailable()
    {
        var source = new[] { CreatePoi(0) };

        var projected = VisitorPoiDistanceProjector.Apply(source, VisitorLocationSnapshot.Disabled());

        Assert.Same(source, projected);
        Assert.Same(source[0], projected[0]);
    }

    private static VisitorPoi CreatePoi(int distanceMeters) =>
        new(
            "poi-1", "POI 1", "history", "Lịch sử", "TP.HCM", "Live API", "Description", "Highlight",
            50, 50, distanceMeters, "1:00", "Ready", 10.7607, 106.7033);
}
