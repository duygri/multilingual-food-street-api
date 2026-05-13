using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetMapPoiAudioStatus()
    {
        if (_state.CurrentAudioCue is not null)
        {
            return $"{UiText.LocalizeKnownStatus(_state.AudioStatusLabel)} • {GetCurrentAudioLanguageLabel()}";
        }

        return UiText.FormatNotPlayedPriority(UiText.LocalizeLanguageLabel(_state.CurrentLanguage.Label));
    }

    private string? GetQueuedPoiStatus() =>
        UiText.LocalizeKnownStatus(VisitorMapQueueStatusFormatter.Build(
            _state.SelectedPoi?.Id,
            _state.ActiveProximity,
            _proximityQueueState.QueuedMatch));

    private string GetSelectedPoiDistanceLabel()
    {
        var poi = _state.SelectedPoi;
        if (poi is null)
        {
            return UiText.CalculatingDistanceLabel();
        }

        var distanceMeters = VisitorGeoMath.TryGetCoordinates(_state.CurrentLocation, out var latitude, out var longitude)
            ? VisitorGeoMath.CalculateDistanceMeters(latitude, longitude, poi.Latitude, poi.Longitude)
            : poi.DistanceMeters;

        return FormatPoiDistance(distanceMeters);
    }

    private static string FormatPoiDistance(int distanceMeters)
    {
        if (distanceMeters < 1000)
        {
            return $"{Math.Max(0, distanceMeters)} m";
        }

        return $"{distanceMeters / 1000d:0.#} km";
    }

    private async Task SelectMapPoiLanguageAsync(string languageCode)
    {
        await SelectAudioLanguageAsync(languageCode, keepPlayback: false);
    }
}
