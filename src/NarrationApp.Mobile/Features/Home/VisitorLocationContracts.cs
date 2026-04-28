namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorLocationService
{
    Task<VisitorLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default);
}

public sealed record VisitorLocationSnapshot(
    bool PermissionGranted,
    bool IsLocationAvailable,
    double? Latitude,
    double? Longitude,
    string StatusLabel)
{
    public static VisitorLocationSnapshot Disabled(string statusLabel = "Chưa bật vị trí")
    {
        return new VisitorLocationSnapshot(
            PermissionGranted: false,
            IsLocationAvailable: false,
            Latitude: null,
            Longitude: null,
            StatusLabel: statusLabel);
    }
}

public sealed record VisitorContentLoadRequest(
    bool PreferNearbyPois = false,
    bool RequestLocationPermission = false,
    int RadiusMeters = 500);
