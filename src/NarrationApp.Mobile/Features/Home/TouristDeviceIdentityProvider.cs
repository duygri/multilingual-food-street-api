namespace NarrationApp.Mobile.Features.Home;

public interface ITouristDeviceIdentityProvider
{
    ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default);
}
