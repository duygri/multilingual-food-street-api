using System.Globalization;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorListeningHistoryUpdater
{
    private const string TodayLabel = "Hôm nay";

    public static void UpsertCurrent(
        List<VisitorListeningHistoryDay> historyDays,
        VisitorAudioCue? cue,
        VisitorPoi? selectedPoi,
        int elapsedSeconds,
        int durationSeconds)
    {
        if (cue is not { IsAvailable: true } || selectedPoi is null)
        {
            return;
        }

        if (!string.Equals(cue.PoiId, selectedPoi.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var todayIndex = FindTodayIndex(historyDays);
        var entries = todayIndex >= 0 ? historyDays[todayIndex].Entries.ToList() : [];
        var existingIndex = entries.FindIndex(entry => IsSameAudio(entry, cue));
        var existingEntry = existingIndex >= 0 ? entries[existingIndex] : null;

        if (existingIndex >= 0)
        {
            entries.RemoveAt(existingIndex);
        }

        entries.Insert(0, new VisitorListeningHistoryEntry(
            existingEntry?.Id ?? $"history-{Guid.NewGuid():N}",
            cue.PoiId,
            selectedPoi.Name,
            selectedPoi.CategoryLabel,
            cue.LanguageCode,
            DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture),
            VisitorDurationFormatter.FormatSeconds(cue.DurationSeconds),
            Math.Max(existingEntry?.CompletionPercent ?? 0, CalculateCompletionPercent(elapsedSeconds, durationSeconds))));

        var today = new VisitorListeningHistoryDay(TodayLabel, entries);
        if (todayIndex >= 0)
        {
            historyDays[todayIndex] = today;
            return;
        }

        historyDays.Insert(0, today);
    }

    public static void UpdateProgress(
        List<VisitorListeningHistoryDay> historyDays,
        VisitorAudioCue? cue,
        int elapsedSeconds,
        int durationSeconds)
    {
        if (cue is not { IsAvailable: true })
        {
            return;
        }

        for (var dayIndex = 0; dayIndex < historyDays.Count; dayIndex++)
        {
            var day = historyDays[dayIndex];
            var entries = day.Entries.ToList();
            var entryIndex = entries.FindIndex(entry => IsSameAudio(entry, cue));
            if (entryIndex < 0)
            {
                continue;
            }

            var entry = entries[entryIndex];
            entries[entryIndex] = entry with
            {
                DurationLabel = VisitorDurationFormatter.FormatSeconds(durationSeconds),
                CompletionPercent = Math.Max(entry.CompletionPercent, CalculateCompletionPercent(elapsedSeconds, durationSeconds))
            };
            historyDays[dayIndex] = day with { Entries = entries };
            return;
        }
    }

    private static int FindTodayIndex(IReadOnlyList<VisitorListeningHistoryDay> historyDays)
    {
        for (var index = 0; index < historyDays.Count; index++)
        {
            if (string.Equals(historyDays[index].Label, TodayLabel, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSameAudio(VisitorListeningHistoryEntry entry, VisitorAudioCue cue)
    {
        return string.Equals(entry.PoiId, cue.PoiId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.LanguageCode, cue.LanguageCode, StringComparison.OrdinalIgnoreCase);
    }

    private static int CalculateCompletionPercent(int elapsedSeconds, int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return 0;
        }

        return (int)Math.Clamp(
            Math.Round(elapsedSeconds * 100d / durationSeconds, MidpointRounding.AwayFromZero),
            0d,
            100d);
    }
}
