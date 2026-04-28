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
            .ThenBy(poi => Math.Abs(poi.DistanceMeters - selectedPoi.DistanceMeters))
            .Take(2)
            .ToArray();
    }
}
