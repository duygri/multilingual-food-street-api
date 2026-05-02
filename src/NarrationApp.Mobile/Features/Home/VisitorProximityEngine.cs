namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorProximityMatch(
    string PoiId,
    string PoiName,
    int DistanceMeters,
    int TriggerRadiusMeters,
    int Priority = 1);

public static class VisitorProximityEngine
{
    public static VisitorProximityMatch? Evaluate(VisitorLocationSnapshot location, IReadOnlyList<VisitorPoi> pois)
    {
        return EvaluateCandidates(location, pois).FirstOrDefault();
    }

    public static IReadOnlyList<VisitorProximityMatch> EvaluateCandidates(VisitorLocationSnapshot location, IReadOnlyList<VisitorPoi> pois)
    {
        if (pois.Count == 0
            || !VisitorGeoMath.TryGetCoordinates(location, out var latitude, out var longitude))
        {
            return [];
        }

        var matches = new List<VisitorProximityMatch>();

        foreach (var poi in pois)
        {
            var distanceMeters = VisitorGeoMath.CalculateDistanceMeters(
                latitude,
                longitude,
                poi.Latitude,
                poi.Longitude);

            var triggerRadiusMeters = Math.Max(90, poi.GeofenceRadiusMeters);
            if (distanceMeters > triggerRadiusMeters)
            {
                continue;
            }

            matches.Add(new VisitorProximityMatch(
                poi.Id,
                poi.Name,
                distanceMeters,
                triggerRadiusMeters,
                poi.Priority));
        }

        return matches
            .OrderByDescending(match => match.Priority)
            .ThenBy(match => match.DistanceMeters)
            .ThenBy(match => match.PoiName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
