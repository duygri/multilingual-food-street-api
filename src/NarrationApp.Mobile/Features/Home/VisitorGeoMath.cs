namespace NarrationApp.Mobile.Features.Home;

public static class VisitorGeoMath
{
    private const double EarthRadiusMeters = 6371000d;

    public static bool TryGetCoordinates(VisitorLocationSnapshot? location, out double latitude, out double longitude)
    {
        if (location is not null
            && location.PermissionGranted
            && location.IsLocationAvailable
            && location.Latitude is not null
            && location.Longitude is not null)
        {
            latitude = location.Latitude.Value;
            longitude = location.Longitude.Value;
            return true;
        }

        latitude = 0d;
        longitude = 0d;
        return false;
    }

    public static int CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var lat1Radians = DegreesToRadians(lat1);
        var lat2Radians = DegreesToRadians(lat2);
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLng = DegreesToRadians(lng2 - lng1);

        var haversine =
            Math.Sin(deltaLat / 2d) * Math.Sin(deltaLat / 2d)
            + Math.Cos(lat1Radians) * Math.Cos(lat2Radians)
            * Math.Sin(deltaLng / 2d) * Math.Sin(deltaLng / 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1d - haversine));
        return (int)Math.Round(EarthRadiusMeters * c, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
