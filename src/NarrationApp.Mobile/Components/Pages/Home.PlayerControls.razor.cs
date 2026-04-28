using Microsoft.JSInterop;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task ToggleAudioSpeedAsync()
    {
        _audioSpeedIndex = (_audioSpeedIndex + 1) % AudioSpeedOptions.Length;
        _state.SetAudioPlaybackSpeed(AudioSpeedOptions[_audioSpeedIndex]);
        await JS.InvokeVoidAsync("visitorAudio.setRate", AudioSpeedOptions[_audioSpeedIndex]);
    }

    private async Task SeekAudioBackwardAsync()
    {
        await SeekAudioAsync(-15);
    }

    private async Task SeekAudioForwardAsync()
    {
        await SeekAudioAsync(15);
    }

    private async Task SeekAudioAsync(int offsetSeconds)
    {
        if (_state.CurrentAudioCue is null)
        {
            return;
        }

        await JS.InvokeVoidAsync("visitorAudio.seek", offsetSeconds);
    }

    private async Task RestartAudioAsync()
    {
        if (_state.CurrentAudioCue is null)
        {
            return;
        }

        _state.UpdateAudioProgress(0, _state.AudioDurationSeconds);
        await JS.InvokeVoidAsync("visitorAudio.restart");
    }
}
