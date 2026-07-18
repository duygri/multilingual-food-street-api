namespace NarrationApp.Mobile.Features.Home;

public static class VisitorPoiPresentationOrder
{
    public static IReadOnlyList<VisitorPoi> Apply(IReadOnlyList<VisitorPoi> pois, bool hasValidGps)
    {
        ArgumentNullException.ThrowIfNull(pois);

        var hasReliableGpsDistances = hasValidGps && pois.All(poi => poi.HasReliableDistance);

        return hasReliableGpsDistances
            ? pois
                .OrderByDescending(poi => poi.Priority)
                .ThenBy(poi => poi.DistanceMeters)
                .ThenBy(poi => poi.Id, StringComparer.Ordinal)
                .ToArray()
            : pois
                .OrderByDescending(poi => poi.Priority)
                .ThenBy(poi => poi.Id, StringComparer.Ordinal)
                .ToArray();
    }
}
