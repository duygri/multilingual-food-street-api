namespace NarrationApp.Mobile.Features.Home;

public static class VisitorPoiDistanceProjector
{
    public static IReadOnlyList<VisitorPoi> Apply(IReadOnlyList<VisitorPoi> pois, VisitorLocationSnapshot? location)
    {
        if (pois.Count == 0 || !VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            return pois;
        }

        return pois
            .Select(poi => poi with
            {
                DistanceMeters = VisitorGeoMath.CalculateDistanceMeters(
                    latitude,
                    longitude,
                    poi.Latitude,
                    poi.Longitude)
            })
            .ToArray();
    }
}
