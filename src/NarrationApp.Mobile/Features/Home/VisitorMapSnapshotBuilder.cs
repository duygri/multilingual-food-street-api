namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorMapSnapshot(
    double CenterLat,
    double CenterLng,
    double Zoom,
    IReadOnlyList<VisitorMapMarker> Markers,
    VisitorMapUserLocation? UserLocation = null);

public sealed record VisitorMapMarker(
    string Id,
    string Label,
    double Latitude,
    double Longitude,
    bool IsSelected,
    bool IsNearest,
    string Accent);

public sealed record VisitorMapUserLocation(
    double Latitude,
    double Longitude,
    string Label);

public static class VisitorMapSnapshotBuilder
{
    private const int MaxCenterOnUserDistanceMeters = 4000;

    public static VisitorMapSnapshot Build(
        IReadOnlyList<VisitorPoi> pois,
        string? selectedPoiId,
        VisitorLocationSnapshot? location)
    {
        var userLocation = BuildUserLocation(location);
        var visiblePois = GetVisibleMarkerPois(pois, selectedPoiId, location);
        var nearestPoiId = GetNearestPoiId(visiblePois, location);
        var markers = visiblePois
            .Select(poi => new VisitorMapMarker(
                poi.Id,
                poi.Name,
                poi.Latitude,
                poi.Longitude,
                string.Equals(poi.Id, selectedPoiId, StringComparison.OrdinalIgnoreCase),
                poi.Id == nearestPoiId,
                GetAccent(poi.CategoryId)))
            .ToList();

        if (markers.Count == 0)
        {
            return userLocation is not null
                ? new VisitorMapSnapshot(userLocation.Latitude, userLocation.Longitude, 14.8, [], userLocation)
                : new VisitorMapSnapshot(10.7600, 106.7040, 13.4, []);
        }

        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null
            && IsReasonableMapCenter(location, visiblePois))
        {
            return new VisitorMapSnapshot(location.Latitude.Value, location.Longitude.Value, 14.8, markers, userLocation);
        }

        var selectedPoi = visiblePois.FirstOrDefault(poi => string.Equals(poi.Id, selectedPoiId, StringComparison.OrdinalIgnoreCase)) ?? visiblePois[0];
        return new VisitorMapSnapshot(selectedPoi.Latitude, selectedPoi.Longitude, 14.3, markers, userLocation);
    }

    private static IReadOnlyList<VisitorPoi> GetVisibleMarkerPois(
        IReadOnlyList<VisitorPoi> pois,
        string? selectedPoiId,
        VisitorLocationSnapshot? location)
    {
        if (pois.Count == 0)
        {
            return [];
        }

        return pois
            .Where(poi =>
                string.Equals(poi.Id, selectedPoiId, StringComparison.OrdinalIgnoreCase)
                || IsInsideTriggerRadius(poi, location))
            .ToArray();
    }

    private static bool IsInsideTriggerRadius(VisitorPoi poi, VisitorLocationSnapshot? location)
    {
        if (!VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            return false;
        }

        var distanceMeters = VisitorGeoMath.CalculateDistanceMeters(
            latitude,
            longitude,
            poi.Latitude,
            poi.Longitude);
        return distanceMeters <= Math.Max(90, poi.GeofenceRadiusMeters);
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
            .OrderBy(poi => poi.DistanceMeters)
            .Select(poi => poi.Id)
            .FirstOrDefault();
    }

    private static bool IsReasonableMapCenter(VisitorLocationSnapshot location, IReadOnlyList<VisitorPoi> pois)
    {
        if (pois.Count == 0 || !VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            return false;
        }

        var nearestDistance = pois
            .Min(poi => VisitorGeoMath.CalculateDistanceMeters(
                latitude,
                longitude,
                poi.Latitude,
                poi.Longitude));

        return nearestDistance <= MaxCenterOnUserDistanceMeters;
    }

    private static VisitorMapUserLocation? BuildUserLocation(VisitorLocationSnapshot? location)
    {
        return VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude)
            ? new VisitorMapUserLocation(latitude, longitude, "Vị trí của bạn")
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
