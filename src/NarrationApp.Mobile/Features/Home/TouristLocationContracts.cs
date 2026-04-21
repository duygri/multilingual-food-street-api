namespace NarrationApp.Mobile.Features.Home;

public interface ITouristLocationService
{
    Task<TouristLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default);
}

public sealed record TouristLocationSnapshot(
    bool PermissionGranted,
    bool IsLocationAvailable,
    double? Latitude,
    double? Longitude,
    string StatusLabel)
{
    public static TouristLocationSnapshot Disabled(string statusLabel = "Chưa bật vị trí")
    {
        return new TouristLocationSnapshot(
            PermissionGranted: false,
            IsLocationAvailable: false,
            Latitude: null,
            Longitude: null,
            StatusLabel: statusLabel);
    }
}

public sealed record TouristContentLoadRequest(
    bool PreferNearbyPois = false,
    bool RequestLocationPermission = false,
    int RadiusMeters = 500);
