using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private IReadOnlyList<VisitorSettingsStat> GetSettingsStats() =>
        VisitorSettingsPresentationFormatter.CreateSettingsStats(
            _state.ListeningHistoryDays.SelectMany(day => day.Entries).Select(entry => entry.PoiId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            _state.Tours.Count,
            _state.CachedAudioItems.Count);

    private string GetAudioCacheSummary() =>
        VisitorSettingsPresentationFormatter.FormatAudioCacheSummary(
            _state.CachedAudioItems.Count,
            _state.CachedAudioItems.Sum(item => item.SizeMb));

    private string GetListeningHistoryHeadline()
    {
        var entries = _state.ListeningHistoryDays.SelectMany(day => day.Entries).ToList();
        var completed = entries.Count(entry => entry.CompletionPercent >= 100);
        return VisitorSettingsPresentationFormatter.FormatListeningHistoryHeadline(entries.Count, completed);
    }
}
