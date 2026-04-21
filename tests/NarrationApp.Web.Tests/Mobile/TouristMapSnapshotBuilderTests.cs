using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristMapSnapshotBuilderTests
{
    [Fact]
    public void Build_UsesCurrentLocationAsCenterWhenAvailable()
    {
        var pois = new[]
        {
            new TouristPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new TouristPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.768, 106.7068)
        };
        var location = new TouristLocationSnapshot(true, true, 10.7615, 106.7060, "Đã định vị");

        var snapshot = TouristMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", location);

        Assert.Equal(10.7615, snapshot.CenterLat);
        Assert.Equal(106.7060, snapshot.CenterLng);
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
    }

    [Fact]
    public void Build_FallsBackToSelectedPoiWhenLocationIsUnavailable()
    {
        var pois = new[]
        {
            new TouristPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new TouristPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.768, 106.7068)
        };

        var snapshot = TouristMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", TouristLocationSnapshot.Disabled());

        Assert.Equal(10.768, snapshot.CenterLat);
        Assert.Equal(106.7068, snapshot.CenterLng);
        Assert.Equal(2, snapshot.Markers.Count);
    }
}
