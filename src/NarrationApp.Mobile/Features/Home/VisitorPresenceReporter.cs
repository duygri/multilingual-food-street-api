using System.Globalization;
using System.Net.Http.Json;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorPresenceReporter
{
    Task TrackAsync(CancellationToken cancellationToken = default);
}

public sealed class VisitorPresenceReporter(
    HttpClient httpClient,
    IVisitorDeviceIdentityProvider deviceIdentityProvider) : IVisitorPresenceReporter
{
    public async Task TrackAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = await deviceIdentityProvider.GetDeviceIdAsync(cancellationToken);
        var request = new
        {
            DeviceId = deviceId,
            Source = "mobile-presence",
            PreferredLanguage = CultureInfo.CurrentUICulture.Name
        };

        using var response = await httpClient.PostAsJsonAsync("api/visitor-presence/heartbeat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
