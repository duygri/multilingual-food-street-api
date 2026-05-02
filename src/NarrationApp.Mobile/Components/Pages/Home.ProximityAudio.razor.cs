using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ApplyProximityNarrationAsync(
        VisitorProximityMatch? previousProximity,
        VisitorProximityMatch? nextProximity)
    {
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
            await PauseCurrentAudioBestEffortAsync();
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Paused, "Đã rời vùng phát tự động");
            ClearCurrentAutoNarration();
        }
    }

    private async Task PauseCurrentAudioBestEffortAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("visitorAudio.pause");
        }
        catch (JSException)
        {
            // Best-effort pause – audio may not be loaded yet.
        }
    }
}
