using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapRenderStateTests
{
    [Fact]
    public void ShouldRender_ReturnsFalseForIdenticalSnapshot()
    {
        var state = new VisitorMapRenderState();
        var snapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff"),
                new VisitorMapMarker("poi-2", "Bến Nhà Rồng", 10.768000, 106.706800, true, false, "#1ed6af")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));

        Assert.True(state.ShouldRender(snapshot));
        Assert.False(state.ShouldRender(snapshot));
    }

    [Fact]
    public void Reset_AllowsSameSnapshotToRenderAgain()
    {
        var state = new VisitorMapRenderState();
        var snapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));

        Assert.True(state.ShouldRender(snapshot));
        Assert.False(state.ShouldRender(snapshot));

        state.Reset();

        Assert.True(state.ShouldRender(snapshot));
    }

    [Fact]
    public void ShouldRender_ReturnsTrueWhenMarkerSelectionChanges()
    {
        var state = new VisitorMapRenderState();
        var initialSnapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));
        var updatedSnapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, true, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));

        Assert.True(state.ShouldRender(initialSnapshot));
        Assert.True(state.ShouldRender(updatedSnapshot));
    }

    [Fact]
    public void ShouldRender_ReturnsTrueWhenUserLocationChanges()
    {
        var state = new VisitorMapRenderState();
        var initialSnapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));
        var updatedSnapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.761200, 106.705700, "Vị trí của bạn"));

        Assert.True(state.ShouldRender(initialSnapshot));
        Assert.True(state.ShouldRender(updatedSnapshot));
    }

    [Fact]
    public void ShouldRender_ReturnsFalseForMinorUserLocationJitter()
    {
        var state = new VisitorMapRenderState();
        var initialSnapshot = new VisitorMapSnapshot(
            10.760900,
            106.705400,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760900, 106.705400, "Vị trí của bạn"));
        var jitteredSnapshot = new VisitorMapSnapshot(
            10.760904,
            106.705404,
            14.8,
            [
                new VisitorMapMarker("poi-1", "Cầu Khánh Hội", 10.760900, 106.705400, false, true, "#59b8ff")
            ],
            new VisitorMapUserLocation(10.760904, 106.705404, "Vị trí của bạn"));

        Assert.True(state.ShouldRender(initialSnapshot));
        Assert.False(state.ShouldRender(jitteredSnapshot));
    }
}
