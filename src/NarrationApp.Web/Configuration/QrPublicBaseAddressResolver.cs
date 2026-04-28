namespace NarrationApp.Web.Configuration;

public static class QrPublicBaseAddressResolver
{
    public static Uri Resolve(string? configuredQrPublicBaseUrl, string hostBaseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostBaseAddress);

        var hostUri = new Uri(hostBaseAddress, UriKind.Absolute);
        if (string.IsNullOrWhiteSpace(configuredQrPublicBaseUrl))
        {
            return hostUri;
        }

        var trimmed = configuredQrPublicBaseUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri(hostUri, trimmed);
    }
}
