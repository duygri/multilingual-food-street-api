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
        Assert.Equal(["poi-1", "poi-2"], snapshot.Markers.Select(marker => marker.Id));
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
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

        Assert.Equal(["poi-1", "poi-2", "poi-3"], snapshot.Markers.Select(marker => marker.Id));
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
        Assert.Equal("poi-1", Assert.Single(snapshot.Markers.Where(marker => marker.IsNearest)).Id);
    }

    [Fact]
    public void Build_ShowsNearbyPoisOnMapWithoutRequiringTriggerRadius()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-near", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054, GeofenceRadiusMeters: 30),
            new VisitorPoi("poi-just-outside", "Ốc Oanh", "food", "Hải sản", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.76125, 106.7055, GeofenceRadiusMeters: 30),
            new VisitorPoi("poi-far", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068, GeofenceRadiusMeters: 30),
            new VisitorPoi("poi-selected", "Phố đêm Xóm Chiếu", "night", "Đêm", "Quận 4", "Live API", "desc", "highlight", 74, 70, 420, "2:20", "Sẵn sàng", 10.7597, 106.7008, GeofenceRadiusMeters: 30)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-selected", location);

        Assert.Equal(["poi-near", "poi-just-outside", "poi-far", "poi-selected"], snapshot.Markers.Select(marker => marker.Id));
        Assert.True(snapshot.Markers.Single(marker => marker.Id == "poi-selected").IsSelected);
        Assert.True(snapshot.Markers.Single(marker => marker.Id == "poi-near").IsNearest);
    }

    [Fact]
    public void Build_KeepsAllPoiMarkersVisibleWhenUserIsNearPoiCluster()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-near", "Ốc Oanh", "hai-san", "Hải sản", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-mid", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068),
            new VisitorPoi("poi-far", "Điểm dừng Vĩnh Hội", "tour", "Tour", "Quận 4", "Live API", "desc", "highlight", 74, 70, 420, "2:20", "Sẵn sàng", 10.7950, 106.7450)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: null, location);

        Assert.Equal(["poi-near", "poi-mid", "poi-far"], snapshot.Markers.Select(marker => marker.Id));
        Assert.Equal("poi-near", Assert.Single(snapshot.Markers.Where(marker => marker.IsNearest)).Id);
    }

    [Fact]
    public void Build_ShowsPoiMarkersWhenLocationUnavailableAndNothingIsSelected()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
            new VisitorPoi("poi-2", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 42, 48, 210, "2:44", "Sẵn sàng", 10.7680, 106.7068)
        };

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: null, VisitorLocationSnapshot.Disabled());

        Assert.Equal(["poi-1", "poi-2"], snapshot.Markers.Select(marker => marker.Id));
    }

    [Fact]
    public void Build_DoesNotLabelAnUnreliableFallbackDistanceAsNearest()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "One", "history", "Lịch sử", "TP.HCM", "Live", "desc", "highlight", 10, 10, 0, "1:00", "Ready", 10.1, 106.1),
            new VisitorPoi("poi-2", "Two", "history", "Lịch sử", "TP.HCM", "Live", "desc", "highlight", 20, 20, 0, "1:00", "Ready", 10.2, 106.2)
        };

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, null, VisitorLocationSnapshot.Disabled());

        Assert.DoesNotContain(snapshot.Markers, marker => marker.IsNearest);
    }

    [Fact]
    public void Build_CarriesPoiGeofenceRadiusIntoMapMarkers()
    {
        var pois = new[]
        {
            new VisitorPoi(
                "poi-1",
                "Ốc Oanh",
                "hai-san",
                "Hải sản",
                "Quận 4",
                "Live API",
                "desc",
                "highlight",
                18,
                52,
                180,
                "3:12",
                "Sẵn sàng",
                10.7609,
                106.7054,
                GeofenceRadiusMeters: 85)
        };

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-1", VisitorLocationSnapshot.Disabled());

        Assert.Equal(85, Assert.Single(snapshot.Markers).RadiusMeters);
    }

    [Fact]
    public void Build_CarriesWalkingRouteIntoSnapshot()
    {
        var pois = new[]
        {
            new VisitorPoi("poi-1", "Ốc Oanh", "hai-san", "Hải sản", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054)
        };
        var location = new VisitorLocationSnapshot(true, true, 10.7608, 106.7055, "Đã định vị");
        var route = new VisitorMapRoute(
            [
                new VisitorMapRoutePoint(10.7608, 106.7055),
                new VisitorMapRoutePoint(10.7609, 106.7054)
            ],
            "Đi bộ 32 m • khoảng 1 phút");

        var snapshot = VisitorMapSnapshotBuilder.Build(pois, selectedPoiId: "poi-1", location, route);

        Assert.Same(route, snapshot.Route);
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
        Assert.Equal(["poi-1", "poi-2"], snapshot.Markers.Select(marker => marker.Id));
        Assert.Equal("poi-2", Assert.Single(snapshot.Markers.Where(marker => marker.IsSelected)).Id);
    }
}
