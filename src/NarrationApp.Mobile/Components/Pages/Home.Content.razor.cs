using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task LoadContentAsync(bool requestLocationPermission = false, bool preferNearbyPois = false)
    {
        var previousProximity = _state.ActiveProximity;
        _isContentLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await ContentService.LoadAsync(
                new VisitorContentLoadRequest(
                    PreferNearbyPois: preferNearbyPois,
                    RequestLocationPermission: requestLocationPermission));

            _state.UpdateLocation(result.Location);
            _state.ApplyContent(result.Content, result.IsFallback, result.SourceLabel, result.Message);
            var nextProximity = VisitorProximityEngine.Evaluate(result.Location, _state.Pois);
            var autoNarrationDecision = VisitorAutoNarrationDecider.Evaluate(
                previousProximity,
                nextProximity,
                _isAutoPlayingFromProximity,
                _state.AudioPlaybackState);

            _state.ApplyProximityFocus(nextProximity);

            if (autoNarrationDecision.ShouldResetAutoPlayedPoiId)
            {
                _lastAutoPlayedPoiId = null;
            }

            await PrepareSelectedPoiAudioAsync(autoPlay: _state.AudioPreferences.AutoPlayEnabled && _state.ActiveProximity is not null);

            if (_state.AudioPreferences.AutoPlayEnabled && autoNarrationDecision.ShouldPauseCurrentAudio)
            {
                await JS.InvokeVoidAsync("visitorAudio.pause");
                _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Paused, "Đã rời vùng phát tự động");
                _isAutoPlayingFromProximity = false;
            }
        }
        finally
        {
            _isContentLoading = false;
        }
    }
}
