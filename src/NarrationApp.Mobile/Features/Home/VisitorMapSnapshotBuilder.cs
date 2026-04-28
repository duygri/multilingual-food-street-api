namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorMapSnapshot(
    double CenterLat,
    double CenterLng,
    double Zoom,
    IReadOnlyList<VisitorMapMarker> Markers);

public sealed record VisitorMapMarker(
    string Id,
    string Label,
    double Latitude,
    double Longitude,
    bool IsSelected,
    bool IsNearest,
    string Accent);

public static class VisitorMapSnapshotBuilder
{
    public static VisitorMapSnapshot Build(
        IReadOnlyList<VisitorPoi> pois,
        string? selectedPoiId,
        VisitorLocationSnapshot? location)
    {
        var nearestPoiId = GetNearestPoiId(pois, location);
        var markers = pois
            .Select(poi => new VisitorMapMarker(
                poi.Id,
                poi.Name,
                poi.Latitude,
                poi.Longitude,
                poi.Id == selectedPoiId,
                poi.Id == nearestPoiId,
                GetAccent(poi.CategoryId)))
            .ToList();

        if (markers.Count == 0)
        {
            return new VisitorMapSnapshot(10.7600, 106.7040, 13.4, []);
        }

        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null)
        {
            return new VisitorMapSnapshot(location.Latitude.Value, location.Longitude.Value, 14.8, markers);
        }

        var selectedPoi = pois.FirstOrDefault(poi => poi.Id == selectedPoiId) ?? pois[0];
        return new VisitorMapSnapshot(selectedPoi.Latitude, selectedPoi.Longitude, 14.3, markers);
    }

    private static string? GetNearestPoiId(IReadOnlyList<VisitorPoi> pois, VisitorLocationSnapshot? location)
    {
        if (pois.Count == 0)
        {
            return null;
        }

        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null)
        {
            return pois
                .OrderBy(poi => CalculateDistanceMeters(
                    location.Latitude.Value,
                    location.Longitude.Value,
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

    private static int CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6371000d;

        var lat1Radians = DegreesToRadians(lat1);
        var lat2Radians = DegreesToRadians(lat2);
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLng = DegreesToRadians(lng2 - lng1);

        var haversine =
            Math.Sin(deltaLat / 2d) * Math.Sin(deltaLat / 2d)
            + Math.Cos(lat1Radians) * Math.Cos(lat2Radians)
            * Math.Sin(deltaLng / 2d) * Math.Sin(deltaLng / 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1d - haversine));
        return (int)Math.Round(earthRadiusMeters * c, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static string GetAccent(string categoryId) =>
        categoryId switch
        {
            "food" => "#1ed6af",
            "river" => "#59b8ff",
            "night" => "#ff9b4d",
            _ => "#9dd0ff"
        };
}
