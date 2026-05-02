namespace NarrationApp.Mobile.Features.Home;

public static class VisitorAndroidRuntimeDetector
{
    public static bool LooksLikeEmulator(
        string? fingerprint,
        string? model,
        string? manufacturer,
        string? brand,
        string? device,
        string? product,
        string? hardware)
    {
        return StartsWithAny(fingerprint, "generic", "unknown", "sdk_gphone")
            || ContainsAny(model, "emulator", "android sdk built for x86", "sdk_gphone", "sdk_phone")
            || ContainsAny(manufacturer, "genymotion")
            || StartsWithAny(brand, "generic", "android")
            || StartsWithAny(device, "generic", "emulator")
            || ContainsAny(product, "sdk", "emulator", "simulator")
            || ContainsAny(hardware, "goldfish", "ranchu", "cutf_cvm");
    }

    private static bool StartsWithAny(string? value, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string? value, params string[] fragments)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
