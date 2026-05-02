namespace NarrationApp.Mobile.Features.Home;

public sealed class VisitorMapOptions
{
    public string AccessToken { get; init; } = "YOUR_MAPBOX_ACCESS_TOKEN_HERE";

    public string StyleUrl { get; init; } = "mapbox://styles/mapbox/light-v11";
}
