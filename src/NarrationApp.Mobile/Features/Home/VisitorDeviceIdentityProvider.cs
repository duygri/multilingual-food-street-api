namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorDeviceIdentityProvider
{
    ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default);
}
