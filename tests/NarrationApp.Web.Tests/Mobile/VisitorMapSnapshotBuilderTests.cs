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
        Assert.NotNull(snapshot.UserLocation);
        Assert.Equal(10.7615, snapshot.UserLocation!.Latitude);
        Assert.Equal(106.7060, snapshot.UserLocation.Longitude);
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
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers).Id);
    }

    [Fact]
    public void Build_OnlyKeepsVisiblePoisAndFlagsNearestMarker()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068),
            new VisitorPoi("poi-3", "Phố đêm Xóm Chiếu", "night", "Đêm", "Quận 4", "Live API", "desc", "highlight", 74, 70, 420, "2:20", "Sẵn sàng", 10.7597, 106.7008)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", location);

        Assert.Equal(["poi-1", "poi-2"], snapshot.Markers.Select(marker => marker.Id));
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
        Assert.Equal("poi-1", Assert.Single(snapshot.Markers.Where(marker => marker.IsNearest)).Id);
    }

    [Fact]
    public void Build_HidesPoisOutsideTriggerRadiusUnlessTheyAreSelected()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-near", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054, GeofenceRadiusMeters: 30),
            new VisitorPoi("poi-far", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068, GeofenceRadiusMeters: 30),
            new VisitorPoi("poi-selected", "Phố đêm Xóm Chiếu", "night", "Đêm", "Quận 4", "Live API", "desc", "highlight", 74, 70, 420, "2:20", "Sẵn sàng", 10.7597, 106.7008, GeofenceRadiusMeters: 30)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-selected", location);

        Assert.Equal(["poi-near", "poi-selected"], snapshot.Markers.Select(marker => marker.Id));
        Assert.True(snapshot.Markers.Single(marker => marker.Id == "poi-selected").IsSelected);
        Assert.True(snapshot.Markers.Single(marker => marker.Id == "poi-near").IsNearest);
    }

    [Fact]
    public void Build_HidesAllPoiMarkersWhenLocationUnavailableAndNothingIsSelected()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068)
        };

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: null, VisitorLocationSnapshot.Disabled());

        Assert.Empty(snapshot.Markers);
    }

    [Fact]
    public void Build_FallsBackToPoiClusterWhenCurrentLocationIsTooFarAway()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Ốc Oanh", "hai-san", "Hải sản", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Ốc Đào", "hai-san", "Hải sản", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068)
        };
        var farAwayLocation = new VisitorLocationSnapshot(true, true, 37.4220, -122.0840, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-2", farAwayLocation);

        Assert.Equal(10.7680, snapshot.CenterLat);
        Assert.Equal(106.7068, snapshot.CenterLng);
        Assert.NotNull(snapshot.UserLocation);
        Assert.Equal(37.4220, snapshot.UserLocation!.Latitude);
        Assert.Equal(-122.0840, snapshot.UserLocation.Longitude);
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers).Id);
    }
}
