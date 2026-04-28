using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorMapSnapshotBuilderTests
{
    [Fact]
    public void Build_UsesCurrentLocationAsCenterWhenAvailable()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.768, 106.7068)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7615, 106.7060, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", location);

        Assert.Equal(10.7615, snapshot.CenterLat);
        Assert.Equal(106.7060, snapshot.CenterLng);
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
    }

    [Fact]
    public void Build_FallsBackToSelectedPoiWhenLocationIsUnavailable()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.768, 106.7068)
        };

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", VisitorLocationSnapshot.Disabled());

        Assert.Equal(10.768, snapshot.CenterLat);
        Assert.Equal(106.7068, snapshot.CenterLng);
        Assert.Equal(2, snapshot.Markers.Count);
    }

    [Fact]
    public void Build_KeepsAllProvidedPoisAndFlagsNearestMarker()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068),
            new VisitorPoi("poi-3", "Phố đêm Xóm Chiếu", "night", "Đêm", "Quận 4", "Live API", "desc", "highlight", 74, 70, 420, "2:20", "Sẵn sàng", 10.7597, 106.7008)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", location);

        Assert.Equal(3, snapshot.Markers.Count);
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
        Assert.Equal("poi-1", Assert.Single(snapshot.Markers.Where(marker => marker.IsNearest)).Id);
    }
}
