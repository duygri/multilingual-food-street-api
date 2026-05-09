namespace NarrationApp.Web.Configuration;

public sealed class AnalyticsMapOptions
{
    public string AccessToken { get; init; } = string.Empty;

    public string StyleUrl { get; init; } = "mapbox://styles/mapbox/streets-v12";

    public bool HasAccessToken =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && !AccessToken.Contains("YOUR_MAPBOX_ACCESS_TOKEN", StringComparison.OrdinalIgnoreCase);
}
