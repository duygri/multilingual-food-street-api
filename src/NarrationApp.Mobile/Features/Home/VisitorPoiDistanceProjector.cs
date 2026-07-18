namespace NarrationApp.Mobile.Features.Home;

public static class VisitorPoiDistanceProjector
{
    public static IReadOnlyList<VisitorPoi> Apply(IReadOnlyList<VisitorPoi> pois, VisitorLocationSnapshot? location)
    {
        if (pois.Count == 0)
        {
            return pois;
        }

        if (!VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            if (!pois.Any(poi => poi.HasReliableDistance || poi.DistanceMeters != 0))
            {
                return pois;
            }

            return pois
                .Select(poi => poi with
                {
                    DistanceMeters = 0,
                    HasReliableDistance = false
                })
                .ToArray();
        }

        return pois
            .Select(poi => poi with
            {
                DistanceMeters = VisitorGeoMath.CalculateDistanceMeters(
                    latitude,
                    longitude,
                    poi.Latitude,
                    poi.Longitude),
                HasReliableDistance = true
            })
            .ToArray();
    }
}
