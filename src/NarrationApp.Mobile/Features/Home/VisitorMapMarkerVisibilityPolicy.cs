namespace NarrationApp.Mobile.Features.Home;

public static class VisitorMapMarkerVisibilityPolicy
{
    public const int MaxCenterOnUserDistanceMeters = 4000;

    public static IReadOnlyList<VisitorPoi> GetVisiblePois(
        IReadOnlyList<VisitorPoi> pois,
        string? selectedPoiId,
        VisitorLocationSnapshot? location)
    {
        _ = selectedPoiId;
        _ = location;

        return pois.Count == 0 ? [] : pois.ToArray();
    }

    public static bool IsLocationNearPoiCluster(
        IReadOnlyList<VisitorPoi> pois,
        VisitorLocationSnapshot? location)
    {
        return VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude)
            && IsLocationNearPoiCluster(pois, latitude, longitude);
    }

    private static bool IsLocationNearPoiCluster(
        IReadOnlyList<VisitorPoi> pois,
        double latitude,
        double longitude)
    {
        if (pois.Count == 0)
        {
            return false;
        }

        return GetNearestPoiDistanceMeters(pois, latitude, longitude) <= MaxCenterOnUserDistanceMeters;
    }

    private static double GetNearestPoiDistanceMeters(
        IReadOnlyList<VisitorPoi> pois,
        double latitude,
        double longitude)
    {
        return pois.Min(poi => VisitorGeoMath.CalculateDistanceMeters(
            latitude,
            longitude,
            poi.Latitude,
            poi.Longitude));
    }
}
