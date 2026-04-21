namespace NarrationApp.Web.Configuration;

public static class ApiBaseAddressResolver
{
    public static Uri Resolve(string? configuredApiBaseUrl, string hostBaseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostBaseAddress);

        var hostUri = new Uri(hostBaseAddress, UriKind.Absolute);
        if (string.IsNullOrWhiteSpace(configuredApiBaseUrl))
        {
            return hostUri;
        }

        var trimmed = configuredApiBaseUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri(hostUri, trimmed);
    }
}
