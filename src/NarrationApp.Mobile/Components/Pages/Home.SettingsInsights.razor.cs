using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private IReadOnlyList<VisitorSettingsStat> GetSettingsStats() =>
        UiText.CreateSettingsStats(
            _state.ListeningHistoryDays.SelectMany(day => day.Entries).Select(entry => entry.PoiId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            _state.Tours.Count,
            _state.CachedAudioItems.Count);

    private string GetAudioCacheSummary() =>
        UiText.FormatOfflinePackSummary(
            _state.Pois.Count,
            _state.Tours.Count,
            _state.CachedAudioItems.Count,
            _state.CachedAudioItems.Sum(item => item.SizeMb));

    private string GetListeningHistoryHeadline()
    {
        var entries = _state.ListeningHistoryDays.SelectMany(day => day.Entries).ToList();
        var completed = entries.Count(entry => entry.CompletionPercent >= 100);
        return UiText.FormatListeningHistoryHeadline(entries.Count, completed);
    }
}
