using System.Text;
using System.Text.RegularExpressions;

namespace NarrationApp.Mobile.Features.Home;

public static partial class VisitorDeviceIdFormatter
{
    private static readonly Regex LegacyOpaqueDeviceIdPattern = LegacyOpaqueDeviceIdRegex();

    public static bool LooksLikeLegacyOpaqueDeviceId(string? deviceId)
    {
        return !string.IsNullOrWhiteSpace(deviceId)
            && LegacyOpaqueDeviceIdPattern.IsMatch(deviceId.Trim());
    }

    public static string Build(
        string platform,
        string manufacturer,
        string model,
        bool isEmulator,
        string suffix)
    {
        var normalizedPlatform = NormalizeToken(platform, fallback: "device");
        var normalizedKind = isEmulator ? "emulator" : "device";
        var normalizedManufacturer = NormalizeToken(manufacturer, fallback: string.Empty);
        var normalizedModel = NormalizeToken(model, fallback: string.Empty);
        var normalizedSuffix = NormalizeToken(suffix, fallback: "guest");

        var tokens = new List<string> { normalizedPlatform, normalizedKind };
        if (!string.IsNullOrWhiteSpace(normalizedManufacturer))
        {
            tokens.Add(normalizedManufacturer);
        }

        if (!string.IsNullOrWhiteSpace(normalizedModel) && !string.Equals(normalizedModel, normalizedManufacturer, StringComparison.Ordinal))
        {
            tokens.Add(normalizedModel);
        }

        tokens.Add(normalizedSuffix);
        return string.Join("-", tokens.Where(static token => !string.IsNullOrWhiteSpace(token)));
    }

    public static string ExtractLegacySuffix(string legacyDeviceId)
    {
        var trimmed = legacyDeviceId.Trim();
        return trimmed.Length >= 6 ? trimmed[^6..] : trimmed;
    }

    private static string NormalizeToken(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var builder = new StringBuilder(raw.Length);
        var pendingDash = false;
        foreach (var ch in raw.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(ch);
                pendingDash = false;
            }
            else
            {
                pendingDash = builder.Length > 0;
            }
        }

        return builder.Length == 0 ? fallback : builder.ToString();
    }

    [GeneratedRegex("^[a-z]+-[0-9a-f]{32}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyOpaqueDeviceIdRegex();
}
