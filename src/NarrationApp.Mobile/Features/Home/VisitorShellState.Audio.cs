using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void SetAudioCue(VisitorAudioCue cue)
    {
        CurrentAudioCue = cue;
        AudioStatusLabel = cue.StatusLabel;
        AudioElapsedSeconds = 0;
        AudioDurationSeconds = cue.DurationSeconds;
        AudioPlaybackState = cue.IsAvailable ? VisitorAudioPlaybackState.Ready : VisitorAudioPlaybackState.Error;
    }

    public void SetAudioPlaybackState(VisitorAudioPlaybackState playbackState, string? statusLabel = null)
    {
        AudioPlaybackState = playbackState;

        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            AudioStatusLabel = statusLabel;
        }

        if (playbackState == VisitorAudioPlaybackState.Playing)
        {
            VisitorListeningHistoryUpdater.UpsertCurrent(
                _listeningHistoryDays,
                CurrentAudioCue,
                SelectedPoi,
                AudioElapsedSeconds,
                AudioDurationSeconds);
        }
    }

    public void UpdateAudioProgress(int elapsedSeconds, int? durationSeconds = null)
    {
        AudioDurationSeconds = durationSeconds is > 0 ? durationSeconds.Value : AudioDurationSeconds;
        AudioElapsedSeconds = Math.Clamp(elapsedSeconds, 0, Math.Max(AudioDurationSeconds, elapsedSeconds));
        VisitorListeningHistoryUpdater.UpdateProgress(
            _listeningHistoryDays,
            CurrentAudioCue,
            AudioElapsedSeconds,
            AudioDurationSeconds);
    }
}
