namespace NarrationApp.Mobile.Features.Home;

public enum VisitorLocationSource
{
    Live,
    LastKnown,
    Disabled,
    Unavailable,
    DeviceGpsOff,
    Unsupported,
    Error
}

public interface IVisitorLocationService
{
    Task<VisitorLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default);
}

public sealed record VisitorLocationSnapshot(
    bool PermissionGranted,
    bool IsLocationAvailable,
    double? Latitude,
    double? Longitude,
    string StatusLabel,
    VisitorLocationSource Source = VisitorLocationSource.Live)
{
    public static VisitorLocationSnapshot Disabled(string statusLabel = "Chưa bật vị trí")
    {
        return new VisitorLocationSnapshot(
            PermissionGranted: false,
            IsLocationAvailable: false,
            Latitude: null,
            Longitude: null,
            StatusLabel: statusLabel,
            Source: VisitorLocationSource.Disabled);
    }
}

public sealed record VisitorContentLoadRequest(
    bool PreferNearbyPois = false,
    bool RequestLocationPermission = false,
    int RadiusMeters = 500);
