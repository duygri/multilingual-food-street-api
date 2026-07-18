namespace NarrationApp.Mobile.Features.Home;

public static class VisitorRelatedPoiSelector
{
    public static IReadOnlyList<VisitorPoi> Select(IReadOnlyList<VisitorPoi> pois, VisitorPoi? selectedPoi)
    {
        if (selectedPoi is null)
        {
            return [];
        }

        return pois
            .Where(poi => !string.Equals(poi.Id, selectedPoi.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(poi => poi.CategoryId == selectedPoi.CategoryId)
            .ThenByDescending(poi => poi.District == selectedPoi.District)
            .ThenBy(poi => poi.HasReliableDistance && selectedPoi.HasReliableDistance
                ? Math.Abs(poi.DistanceMeters - selectedPoi.DistanceMeters)
                : int.MaxValue)
            .ThenByDescending(poi => poi.Priority)
            .ThenBy(poi => poi.Id, StringComparer.Ordinal)
            .Take(2)
            .ToArray();
    }
}
