namespace NarrationApp.Mobile.Features.Home;

public sealed class TouristMapOptions
{
    public string AccessToken { get; init; } = "YOUR_MAPBOX_ACCESS_TOKEN";

    public string StyleUrl { get; init; } = "mapbox://styles/mapbox/dark-v11";
}
