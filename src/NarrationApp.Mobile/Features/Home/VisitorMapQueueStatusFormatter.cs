namespace NarrationApp.Mobile.Features.Home;

public static class VisitorMapQueueStatusFormatter
{
    public static string? Build(
        string? selectedPoiId,
        VisitorProximityMatch? activeProximity,
        VisitorProximityMatch? queuedMatch)
    {
        if (queuedMatch is null
            || string.IsNullOrWhiteSpace(selectedPoiId)
            || activeProximity is null
            || !string.Equals(activeProximity.PoiId, selectedPoiId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"Tiếp theo nếu còn đứng trong vùng: {queuedMatch.PoiName} • ưu tiên {queuedMatch.Priority}";
    }
}
