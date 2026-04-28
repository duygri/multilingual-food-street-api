using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Services;

public sealed class DeviceVisitorIdentityProvider : IVisitorDeviceIdentityProvider
{
    private const string DeviceIdKey = "visitor.device-id";

    public ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        var existingDeviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingDeviceId))
        {
            return ValueTask.FromResult(existingDeviceId);
        }

        var platform = DeviceInfo.Current.Platform.ToString().ToLowerInvariant();
        var deviceId = $"{platform}-{Guid.NewGuid():N}";
        Preferences.Default.Set(DeviceIdKey, deviceId);
        return ValueTask.FromResult(deviceId);
    }
}
