using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    [JSInvokable]
    public Task SelectPoiFromMap(string poiId)
    {
        var decision = VisitorMapMarkerTapBehavior.Decide(_state.SelectedPoiId, poiId, _state.ShowPoiSheet);
        if (decision == VisitorMapMarkerTapAction.OpenDetail)
        {
            return InvokeAsync(async () =>
            {
                await OpenPoiDetailAsync(poiId);
                StateHasChanged();
            });
        }

        _state.OpenPoi(poiId);
        _isAutoPlayingFromProximity = false;
        return InvokeAsync(async () =>
        {
            await PrepareSelectedPoiAudioAsync();
            StateHasChanged();
        });
    }

    [JSInvokable]
    public Task OnAudioStateChanged(string state, string message)
    {
        return InvokeAsync(() =>
        {
            var playbackState = state switch
            {
                "playing" => VisitorAudioPlaybackState.Playing,
                "paused" => VisitorAudioPlaybackState.Paused,
                "ended" => VisitorAudioPlaybackState.Paused,
                "error" => VisitorAudioPlaybackState.Error,
                _ => VisitorAudioPlaybackState.Ready
            };

            _state.SetAudioPlaybackState(playbackState, message);

            if (state is "ended" or "error")
            {
                ClearCurrentAutoNarration();
            }

            StateHasChanged();
        });
    }

    [JSInvokable]
    public Task OnAudioProgressChanged(int currentSeconds, int durationSeconds)
    {
        return InvokeAsync(() =>
        {
            _state.UpdateAudioProgress(currentSeconds, durationSeconds);
            StateHasChanged();
        });
    }
}
