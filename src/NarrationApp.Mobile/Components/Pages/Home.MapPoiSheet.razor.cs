using Microsoft.Maui.ApplicationModel;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetMapPoiAudioStatus()
    {
        if (_state.CurrentAudioCue is not null)
        {
            return $"{_state.AudioStatusLabel} • {GetCurrentAudioLanguageLabel()}";
        }

        return $"Chưa phát • ưu tiên {_state.CurrentLanguage.Label}";
    }

    private string? GetQueuedPoiStatus() =>
        VisitorMapQueueStatusFormatter.Build(
            _state.SelectedPoi?.Id,
            _state.ActiveProximity,
            _proximityQueueState.QueuedMatch);

    private async Task OpenSelectedPoiDirectionsAsync()
    {
        if (_state.SelectedPoi is null)
        {
            return;
        }

        var url = VisitorMapDirectionsLinkBuilder.BuildDirectionsUrl(_state.SelectedPoi);
        await Launcher.Default.OpenAsync(new Uri(url));
    }
}
