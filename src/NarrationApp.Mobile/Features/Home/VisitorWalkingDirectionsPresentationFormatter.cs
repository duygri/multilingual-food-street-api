using System.Globalization;

namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorWalkingDirectionsNotice(
    string Title,
    string StatusLabel,
    string? DistanceLabel,
    string? DurationLabel,
    bool IsLoading,
    bool IsAvailable);

public static class VisitorWalkingDirectionsPresentationFormatter
{
    private const string LoadingStatus = "Đang vẽ đường đi bộ trong app...";

    public static VisitorWalkingDirectionsNotice? Build(
        VisitorPoi? selectedPoi,
        string? routePoiId,
        VisitorMapRoute? route,
        string? statusLabel,
        bool isLoading)
    {
        if (selectedPoi is null
            || !string.Equals(routePoiId, selectedPoi.Id, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var isAvailable = route is not null && !isLoading;
        return new VisitorWalkingDirectionsNotice(
            isLoading ? $"Đang tìm đường tới {selectedPoi.Name}" : $"Dẫn tới {selectedPoi.Name}",
            string.IsNullOrWhiteSpace(statusLabel) ? LoadingStatus : statusLabel,
            route is null ? null : FormatDistance(route.DistanceMeters),
            route is null ? null : FormatDuration(route.DurationMinutes),
            isLoading,
            isAvailable);
    }

    public static string FormatDistance(int distanceMeters) =>
        distanceMeters >= 1000
            ? $"{(distanceMeters / 1000d).ToString("0.0", CultureInfo.InvariantCulture)} km"
            : $"{Math.Max(0, distanceMeters)} m";

    public static string FormatDuration(int durationMinutes)
    {
        var safeMinutes = Math.Max(1, durationMinutes);
        if (safeMinutes < 60)
        {
            return $"{safeMinutes} phút";
        }

        var hours = safeMinutes / 60;
        var minutes = safeMinutes % 60;
        return minutes == 0
            ? $"{hours} giờ"
            : $"{hours} giờ {minutes} phút";
    }
}
