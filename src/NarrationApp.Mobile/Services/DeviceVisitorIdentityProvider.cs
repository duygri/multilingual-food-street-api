using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;
#if ANDROID
using Android.OS;
#endif

namespace NarrationApp.Mobile.Services;

public sealed class DeviceVisitorIdentityProvider : IVisitorDeviceIdentityProvider
{
    private const string DeviceIdKey = "visitor.device-id";
    private const string DeviceInstallSuffixKey = "visitor.device-install-suffix";

    public ValueTask<string> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        var existingDeviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingDeviceId)
            && !VisitorDeviceIdFormatter.LooksLikeLegacyOpaqueDeviceId(existingDeviceId))
        {
            return ValueTask.FromResult(existingDeviceId);
        }

        var suffix = ResolveStableSuffix(existingDeviceId);
        var deviceId = VisitorDeviceIdFormatter.Build(
            DeviceInfo.Current.Platform.ToString(),
            DeviceInfo.Current.Manufacturer,
            DeviceInfo.Current.Model,
            IsAndroidEmulator(),
            suffix);

        Preferences.Default.Set(DeviceIdKey, deviceId);
        return ValueTask.FromResult(deviceId);
    }

    private static string ResolveStableSuffix(string? existingDeviceId)
    {
        var existingSuffix = Preferences.Default.Get(DeviceInstallSuffixKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingSuffix))
        {
            return existingSuffix;
        }

        var nextSuffix = VisitorDeviceIdFormatter.LooksLikeLegacyOpaqueDeviceId(existingDeviceId)
            ? VisitorDeviceIdFormatter.ExtractLegacySuffix(existingDeviceId!)
            : Guid.NewGuid().ToString("N")[..6];

        Preferences.Default.Set(DeviceInstallSuffixKey, nextSuffix);
        return nextSuffix;
    }

    private static bool IsAndroidEmulator()
    {
#if ANDROID
        return VisitorAndroidRuntimeDetector.LooksLikeEmulator(
            Build.Fingerprint,
            Build.Model,
            Build.Manufacturer,
            Build.Brand,
            Build.Device,
            Build.Product,
            Build.Hardware);
#else
        return false;
#endif
    }
}
