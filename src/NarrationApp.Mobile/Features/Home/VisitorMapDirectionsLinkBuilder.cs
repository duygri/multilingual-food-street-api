using System.Globalization;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorMapDirectionsLinkBuilder
{
    public static string BuildDirectionsUrl(VisitorPoi poi)
    {
        ArgumentNullException.ThrowIfNull(poi);

        var destination = string.Create(
            CultureInfo.InvariantCulture,
            $"{poi.Latitude:F4},{poi.Longitude:F4}");

        return $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(destination)}&travelmode=walking";
    }
}
