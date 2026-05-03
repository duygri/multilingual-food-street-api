using Microsoft.JSInterop;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task PlaySelectedPoiAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        if (_state.CurrentAudioCue is null || _state.CurrentAudioCue.PoiId != _state.SelectedPoi.Id)
        {
            await PrepareSelectedPoiAudioAsync();
        }

        if (!_state.CanPlayAudio || _state.CurrentAudioCue is null)
        {
            return;
        }

        _isAutoPlayingFromProximity = false;
        await JS.InvokeVoidAsync("visitorAudio.play", _state.CurrentAudioCue.StreamUrl, _audioBridge);
        await JS.InvokeVoidAsync("visitorAudio.setRate", AudioSpeedOptions[_audioSpeedIndex]);
        _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Playing, $"Đang phát • {_state.CurrentAudioCue.LanguageCode.ToUpperInvariant()}");
        await TrackAudioPlayAsync(_state.CurrentAudioCue);
    }

    private async Task TogglePlaybackAsync()
    {
        if (_state.IsAudioPlaying)
        {
            await JS.InvokeVoidAsync("visitorAudio.pause");
            _state.SetAudioPlaybackState(VisitorAudioPlaybackState.Paused, "Đã tạm dừng");
            return;
        }

        await PlaySelectedPoiAsync();
    }

    private string GetMiniPlayIcon() => _state.IsAudioPlaying ? "❚❚" : "▶";

    private Task TrackAudioPlayAsync(VisitorAudioCue cue)
    {
        return AudioPlayReporter.TrackAsync(cue, _state.CurrentLocation);
    }
}
