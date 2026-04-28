using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SetAudioAutoPlayEnabledAsync(bool isEnabled)
    {
        _state.SetAudioAutoPlayEnabled(isEnabled);
        return Task.CompletedTask;
    }

    private Task SetAudioSpokenAnnouncementsEnabledAsync(bool isEnabled)
    {
        _state.SetAudioSpokenAnnouncementsEnabled(isEnabled);
        return Task.CompletedTask;
    }

    private Task SetAudioAutoAdvanceEnabledAsync(bool isEnabled)
    {
        _state.SetAudioAutoAdvanceEnabled(isEnabled);
        return Task.CompletedTask;
    }

    private async Task SetAudioSourcePreferenceAsync(VisitorAudioSourcePreference preference)
    {
        _state.SetAudioSourcePreference(preference);

        if (_state.SelectedPoi is not null)
        {
            await PrepareSelectedPoiAudioAsync(autoPlay: false);
        }
    }

    private async Task SetAudioPlaybackSpeedAsync(double speed)
    {
        var nextIndex = Array.FindIndex(AudioSpeedOptions, option => Math.Abs(option - speed) < 0.001d);
        if (nextIndex < 0)
        {
            return;
        }

        _audioSpeedIndex = nextIndex;
        _state.SetAudioPlaybackSpeed(speed);
        await JS.InvokeVoidAsync("visitorAudio.setRate", speed);
    }
}
