using System.Reflection;

namespace NarrationApp.Mobile.Features.Home;

public sealed class VisitorMapOptions
{
    public string AccessToken { get; init; } = VisitorMapboxTokenProvider.AccessToken;

    public string StyleUrl { get; init; } = "mapbox://styles/mapbox/light-v11";
}

internal static class VisitorMapboxTokenProvider
{
    private const string DefaultAccessToken = "YOUR_MAPBOX_ACCESS_TOKEN_HERE";
    private const string LocalTokenProviderTypeName = "NarrationApp.Mobile.Features.Home.VisitorMapboxLocalTokenProvider";

    public static string AccessToken => GetLocalAccessToken() ?? DefaultAccessToken;

    private static string? GetLocalAccessToken()
    {
        var providerType = Type.GetType(LocalTokenProviderTypeName, throwOnError: false);
        var tokenProperty = providerType?.GetProperty(
            "AccessToken",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        return tokenProperty?.GetValue(null) as string;
    }
}
