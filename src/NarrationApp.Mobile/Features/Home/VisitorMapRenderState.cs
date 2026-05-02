using System.Globalization;
using System.Text;

namespace NarrationApp.Mobile.Features.Home;

public sealed class VisitorMapRenderState
{
    private string? _lastFingerprint;

    public bool ShouldRender(VisitorMapSnapshot snapshot)
    {
        var fingerprint = CreateFingerprint(snapshot);
        if (string.Equals(_lastFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return false;
        }

        _lastFingerprint = fingerprint;
        return true;
    }

    public void Reset()
    {
        _lastFingerprint = null;
    }

    internal static string CreateFingerprint(VisitorMapSnapshot snapshot)
    {
        var builder = new StringBuilder()
            .Append(snapshot.CenterLat.ToString("F6", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(snapshot.CenterLng.ToString("F6", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(snapshot.Zoom.ToString("F2", CultureInfo.InvariantCulture));

        foreach (var marker in snapshot.Markers)
        {
            builder
                .Append('|')
                .Append(marker.Id)
                .Append(':')
                .Append(marker.Latitude.ToString("F6", CultureInfo.InvariantCulture))
                .Append(':')
                .Append(marker.Longitude.ToString("F6", CultureInfo.InvariantCulture))
                .Append(':')
                .Append(marker.IsSelected ? '1' : '0')
                .Append(':')
                .Append(marker.IsNearest ? '1' : '0')
                .Append(':')
                .Append(marker.Accent);
        }

        if (snapshot.UserLocation is not null)
        {
            builder
                .Append("|user:")
                .Append(snapshot.UserLocation.Latitude.ToString("F6", CultureInfo.InvariantCulture))
                .Append(':')
                .Append(snapshot.UserLocation.Longitude.ToString("F6", CultureInfo.InvariantCulture))
                .Append(':')
                .Append(snapshot.UserLocation.Label);
        }

        return builder.ToString();
    }
}
