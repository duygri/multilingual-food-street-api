namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorMapSnapshot(
    double CenterLat,
    double CenterLng,
    double Zoom,
    IReadOnlyList<VisitorMapMarker> Markers,
    VisitorMapUserLocation? UserLocation = null,
    VisitorMapRoute? Route = null);

public sealed record VisitorMapMarker(
    string Id,
    string Label,
    double Latitude,
    double Longitude,
    bool IsSelected,
    bool IsNearest,
    string Accent,
    int RadiusMeters = 30);

public sealed record VisitorMapUserLocation(
    double Latitude,
    double Longitude,
    string Label);

public sealed record VisitorMapRoute(
    IReadOnlyList<VisitorMapRoutePoint> Points,
    string StatusLabel,
    int DistanceMeters = 0,
    int DurationMinutes = 0,
    string Accent = "#1ed6af");

public sealed record VisitorMapRoutePoint(
    double Latitude,
    double Longitude);

public static class VisitorMapSnapshotBuilder
{
    public static VisitorMapSnapshot Build(
        IReadOnlyList<VisitorPoi> pois,
        string? selectedPoiId,
        VisitorLocationSnapshot? location,
        VisitorMapRoute? route = null,
        string userLocationLabel = "Vị trí của bạn")
    {
        var userLocation = BuildUserLocation(location, userLocationLabel);
        var visiblePois = VisitorMapMarkerVisibilityPolicy.GetVisiblePois(pois, selectedPoiId, location);
        var nearestPoiId = GetNearestPoiId(visiblePois, location);
        var markers = visiblePois
            .Select(poi => new VisitorMapMarker(
                poi.Id,
                poi.Name,
                poi.Latitude,
                poi.Longitude,
                string.Equals(poi.Id, selectedPoiId, StringComparison.OrdinalIgnoreCase),
                poi.Id == nearestPoiId,
                GetAccent(poi.CategoryId),
                Math.Max(0, poi.GeofenceRadiusMeters)))
            .ToList();

        if (markers.Count == 0)
        {
            return userLocation is not null
                ? new VisitorMapSnapshot(userLocation.Latitude, userLocation.Longitude, 14.8, [], userLocation, route)
                : new VisitorMapSnapshot(10.7600, 106.7040, 13.4, []);
        }

        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null
            && VisitorMapMarkerVisibilityPolicy.IsLocationNearPoiCluster(visiblePois, location))
        {
            return new VisitorMapSnapshot(location.Latitude.Value, location.Longitude.Value, 14.8, markers, userLocation, route);
        }

        var selectedPoi = visiblePois.FirstOrDefault(poi => string.Equals(poi.Id, selectedPoiId, StringComparison.OrdinalIgnoreCase)) ?? visiblePois[0];
        return new VisitorMapSnapshot(selectedPoi.Latitude, selectedPoi.Longitude, 14.3, markers, userLocation, route);
    }

    private static string? GetNearestPoiId(IReadOnlyList<VisitorPoi> pois, VisitorLocationSnapshot? location)
    {
        if (pois.Count == 0)
        {
            return null;
        }

        if (VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            return pois
                .OrderBy(poi => VisitorGeoMath.CalculateDistanceMeters(
                    latitude,
                    longitude,
                    poi.Latitude,
                    poi.Longitude))
                .Select(poi => poi.Id)
                .FirstOrDefault();
        }

        return pois
            .Where(poi => poi.HasReliableDistance)
            .OrderBy(poi => poi.DistanceMeters)
            .ThenByDescending(poi => poi.Priority)
            .ThenBy(poi => poi.Id, StringComparer.Ordinal)
            .Select(poi => poi.Id)
            .FirstOrDefault();
    }

    private static VisitorMapUserLocation? BuildUserLocation(VisitorLocationSnapshot? location, string label)
    {
        return VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude)
            ? new VisitorMapUserLocation(latitude, longitude, label)
            : null;
    }

    private static string GetAccent(string categoryId) =>
        categoryId.Trim().ToLowerInvariant() switch
        {
            "food" => "#1ed6af",
            "river" => "#59b8ff",
            "night" => "#ff9b4d",
            var value when value.Contains("bun") || value.Contains("pho") || value.Contains("food") || value.Contains("am-thuc") => "#1ed6af",
            var value when value.Contains("hai-san") || value.Contains("song") || value.Contains("river") || value.Contains("cau") => "#59b8ff",
            var value when value.Contains("an-vat") || value.Contains("snack") || value.Contains("dem") => "#ff9b4d",
            var value when value.Contains("uong") || value.Contains("drink") || value.Contains("coffee") || value.Contains("ca-phe") => "#f6c453",
            _ => "#9dd0ff"
        };
}
