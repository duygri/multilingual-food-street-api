using System.Globalization;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetCurrentAudioLanguageLabel() =>
        _state.CurrentAudioCue is null
            ? _state.CurrentLanguage.Label
            : _state.Languages.FirstOrDefault(language => language.Code == _state.CurrentAudioCue.LanguageCode)?.Label
                ?? _state.CurrentAudioCue.LanguageCode.ToUpperInvariant();

    private string GetAudioSpeedLabel() => VisitorSettingsPresentationFormatter.FormatPlaybackSpeed(AudioSpeedOptions[_audioSpeedIndex]);

    private string GetPoiDetailAudioSubtitle()
    {
        if (_state.CurrentAudioCue is not null)
        {
            return $"{_state.AudioStatusLabel} • {_state.SelectedPoi?.AudioDuration}";
        }

        return $"Recorded • {_state.SelectedPoi?.AudioDuration} • ưu tiên {_state.CurrentLanguage.Label}";
    }

    private IReadOnlyList<string> GetPoiTranscriptParagraphs()
    {
        if (_state.SelectedPoi is null)
        {
            return [];
        }

        var selectedLanguageCode = _state.SelectedLanguageCode;
        var description = _state.SelectedPoi.GetDescriptionForLanguage(selectedLanguageCode);
        var highlight = _state.SelectedPoi.GetHighlightForLanguage(selectedLanguageCode);
        var story = _state.SelectedPoi.GetStoryForLanguage(selectedLanguageCode);

        return
        [
            description,
            highlight,
            story
        ];
    }

    private string GetMiniPlayerSubtitle()
    {
        if (_state.CurrentAudioCue is not null)
        {
            return _state.AudioStatusLabel;
        }

        if (_state.ActiveProximity is not null)
        {
            return $"{_state.ActiveProximity.DistanceMeters}m • chờ audio phù hợp";
        }

        return $"Ưu tiên {_state.CurrentLanguage.Label} • {_state.SelectedPoi?.AudioDuration}";
    }

    private string GetMiniProgressStyle() =>
        $"width: {_state.AudioProgressPercent.ToString("0.##", CultureInfo.InvariantCulture)}%;";
}
