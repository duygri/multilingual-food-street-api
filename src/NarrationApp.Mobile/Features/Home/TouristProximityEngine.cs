namespace NarrationApp.Mobile.Features.Home;

public sealed record TouristProximityMatch(
    string PoiId,
    string PoiName,
    int DistanceMeters,
    int TriggerRadiusMeters);

public static class TouristProximityEngine
{
    public static TouristProximityMatch? Evaluate(TouristLocationSnapshot location, IReadOnlyList<TouristPoi> pois)
    {
        if (!location.PermissionGranted
            || !location.IsLocationAvailable
            || location.Latitude is null
            || location.Longitude is null
            || pois.Count == 0)
        {
            return null;
        }

        TouristProximityMatch? bestMatch = null;

        foreach (var poi in pois)
        {
            var distanceMeters = CalculateDistanceMeters(
                location.Latitude.Value,
                location.Longitude.Value,
                poi.Latitude,
                poi.Longitude);

            var triggerRadiusMeters = Math.Max(90, poi.GeofenceRadiusMeters);
            if (distanceMeters > triggerRadiusMeters)
            {
                continue;
            }

            if (bestMatch is null || distanceMeters < bestMatch.DistanceMeters)
            {
                bestMatch = new TouristProximityMatch(
                    poi.Id,
                    poi.Name,
                    distanceMeters,
                    triggerRadiusMeters);
            }
        }

        return bestMatch;
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
}
