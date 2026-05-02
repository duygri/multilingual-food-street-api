using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SetAudioAutoPlayEnabledAsync(bool isEnabled)
    {
        return ApplySettingsStateChangeAsync(() => _state.SetAudioAutoPlayEnabled(isEnabled));
    }

    private Task SetAudioSpokenAnnouncementsEnabledAsync(bool isEnabled)
    {
        return ApplySettingsStateChangeAsync(() => _state.SetAudioSpokenAnnouncementsEnabled(isEnabled));
    }

    private Task SetAudioAutoAdvanceEnabledAsync(bool isEnabled)
    {
        return ApplySettingsStateChangeAsync(() => _state.SetAudioAutoAdvanceEnabled(isEnabled));
    }

    private async Task SetAudioSourcePreferenceAsync(VisitorAudioSourcePreference preference)
    {
        ClearSettingsFeedback();
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

        ClearSettingsFeedback();
        _audioSpeedIndex = nextIndex;
        _state.SetAudioPlaybackSpeed(speed);
        await JS.InvokeVoidAsync("visitorAudio.setRate", speed);
    }
}
