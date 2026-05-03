using System.Net.Http.Json;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorAudioPlayReporter
{
    Task TrackAsync(VisitorAudioCue cue, VisitorLocationSnapshot location, CancellationToken cancellationToken = default);
}

public sealed class VisitorAudioPlayReporter(
    HttpClient httpClient,
    IVisitorDeviceIdentityProvider deviceIdentityProvider) : IVisitorAudioPlayReporter
{
    public async Task TrackAsync(VisitorAudioCue cue, VisitorLocationSnapshot location, CancellationToken cancellationToken = default)
    {
        if (!cue.IsAvailable || !TryParseServerPoiId(cue.PoiId, out var serverPoiId))
        {
            return;
        }

        try
        {
            var deviceId = await deviceIdentityProvider.GetDeviceIdAsync(cancellationToken);
            var request = new
            {
                DeviceId = deviceId,
                PoiId = serverPoiId,
                EventType = EventType.AudioPlay,
                Source = "mobile-audio",
                ListenDurationSeconds = Math.Max(0, cue.DurationSeconds),
                Lat = location.IsLocationAvailable ? location.Latitude : null,
                Lng = location.IsLocationAvailable ? location.Longitude : null
            };

            using var response = await httpClient.PostAsJsonAsync("api/visit-events", request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Analytics must never interrupt visitor audio playback.
        }
    }

    private static bool TryParseServerPoiId(string poiId, out int serverPoiId)
    {
        serverPoiId = 0;
        return poiId.StartsWith("poi-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(poiId["poi-".Length..], out serverPoiId);
    }
}
