using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorLocationStatusFormatterTests
{
    [Fact]
    public void Build_returns_live_label_for_fresh_gps_coordinates()
    {
        var snapshot = new VisitorLocationSnapshot(
            PermissionGranted: true,
            IsLocationAvailable: true,
            Latitude: 10.7607,
            Longitude: 106.7033,
            StatusLabel: string.Empty,
            Source: VisitorLocationSource.Live);

        var label = VisitorLocationStatusFormatter.Build(snapshot);

        Assert.Equal("GPS live 10.7607, 106.7033", label);
    }

    [Fact]
    public void Build_returns_last_known_label_when_live_fix_is_unavailable()
    {
        var snapshot = new VisitorLocationSnapshot(
            PermissionGranted: true,
            IsLocationAvailable: true,
            Latitude: 37.4220,
            Longitude: -122.0840,
            StatusLabel: string.Empty,
            Source: VisitorLocationSource.LastKnown);

        var label = VisitorLocationStatusFormatter.Build(snapshot);

        Assert.Equal("Vị trí gần nhất 37.4220, -122.0840", label);
    }

    [Fact]
    public void Build_returns_disabled_label_when_permission_is_missing()
    {
        var label = VisitorLocationStatusFormatter.Build(VisitorLocationSnapshot.Disabled());

        Assert.Equal("Chưa cấp quyền vị trí", label);
    }
}
