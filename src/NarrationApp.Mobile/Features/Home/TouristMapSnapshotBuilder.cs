namespace NarrationApp.Mobile.Features.Home;

public sealed record TouristMapSnapshot(
    double CenterLat,
    double CenterLng,
    double Zoom,
    IReadOnlyList<TouristMapMarker> Markers);

public sealed record TouristMapMarker(
    string Id,
    string Label,
    double Latitude,
    double Longitude,
    bool IsSelected,
    string Accent);

public static class TouristMapSnapshotBuilder
{
    public static TouristMapSnapshot Build(
        IReadOnlyList<TouristPoi> pois,
        string? selectedPoiId,
        TouristLocationSnapshot? location)
    {
        var markers = pois
            .Select(poi => new TouristMapMarker(
                poi.Id,
                poi.Name,
                poi.Latitude,
                poi.Longitude,
                poi.Id == selectedPoiId,
                GetAccent(poi.CategoryId)))
            .ToList();

        if (markers.Count == 0)
        {
            return new TouristMapSnapshot(10.7600, 106.7040, 13.4, []);
        }

        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null)
        {
            return new TouristMapSnapshot(location.Latitude.Value, location.Longitude.Value, 14.8, markers);
        }

        var selectedPoi = pois.FirstOrDefault(poi => poi.Id == selectedPoiId) ?? pois[0];
        return new TouristMapSnapshot(selectedPoi.Latitude, selectedPoi.Longitude, 14.3, markers);
    }

    private static string GetAccent(string categoryId) =>
        categoryId switch
        {
            "food" => "#1ed6af",
            "river" => "#59b8ff",
            "night" => "#ff9b4d",
            _ => "#9dd0ff"
        };
}
