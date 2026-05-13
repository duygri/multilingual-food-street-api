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
    public static VisitorWalkingDirectionsNotice? Build(
        VisitorPoi? selectedPoi,
        string? routePoiId,
        VisitorMapRoute? route,
        string? statusLabel,
        bool isLoading,
        VisitorUiText? text = null)
    {
        text ??= VisitorUiTextCatalog.ForLanguage("vi");

        if (selectedPoi is null
            || !string.Equals(routePoiId, selectedPoi.Id, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var isAvailable = route is not null && !isLoading;
        return new VisitorWalkingDirectionsNotice(
            text.FormatDirectionsTitle(selectedPoi.Name, isLoading),
            string.IsNullOrWhiteSpace(statusLabel) ? text.WalkingLoadingStatus() : text.LocalizeKnownStatus(statusLabel),
            route is null ? null : FormatDistance(route.DistanceMeters),
            route is null ? null : text.FormatDurationLabel(route.DurationMinutes),
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
