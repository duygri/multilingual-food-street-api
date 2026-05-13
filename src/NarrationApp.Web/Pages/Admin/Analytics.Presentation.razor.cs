using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Pages.Admin;

public partial class Analytics
{
    private IReadOnlyList<(int Rank, TopPoiDto Item)> RankedTopPois => _topPois.Select((item, index) => (index + 1, item)).ToArray();
    private static IReadOnlyList<HeatmapRangeOption> HeatmapRangeOptions => [new(HeatmapTimeRange.Last7Days, "7 ngày"), new(HeatmapTimeRange.Last24Hours, "24h")];
    private static IReadOnlyList<HeatmapRangeOption> MovementFlowRangeOptions => [new(HeatmapTimeRange.AllTime, "Tất cả"), new(HeatmapTimeRange.Last7Days, "7 ngày"), new(HeatmapTimeRange.Last24Hours, "24h")];
    private double HighestHeatWeight => _heatmap.Count == 0 ? 0d : _heatmap.Max(item => item.Weight);
    private int HighestAnonymousSessions => _movementFlows.Count == 0 ? 0 : _movementFlows.Max(item => item.UniqueSessions);
    private int TotalFlowTraversals => _movementFlows.Sum(item => item.Weight);
    private string HeatmapModeSummary => $"Tất cả event | {GetHeatmapRangeLabel(_selectedHeatmapTimeRange)} | unique session | grid 50m";
    private string MovementFlowModeSummary => $"Tất cả event | {GetHeatmapRangeLabel(_selectedMovementFlowTimeRange)} | min {_minimumMovementFlowSessions:N0} session | peak {HighestAnonymousSessions:N0} session";

    private double BuildTopPoiBarWidth(int visits)
    {
        var maxVisits = _topPois.Count == 0 ? 0 : _topPois.Max(item => item.Visits);
        return maxVisits == 0 ? 0 : Math.Max(12d, visits * 100d / maxVisits);
    }

    private double BuildAverageListenBarWidth(double durationSeconds)
    {
        var maxDuration = _averageListenByPoi.Count == 0 ? 0 : _averageListenByPoi.Max(item => item.AverageListenDurationSeconds);
        return maxDuration <= 0 ? 0 : Math.Max(12d, durationSeconds * 100d / maxDuration);
    }

    private static string GetHeatLabel(int visits) => visits switch { >= 900 => "Cao", >= 500 => "Trung bình", _ => "Thấp" };
    private static StatusTone GetHeatTone(int visits) => visits switch { >= 900 => StatusTone.Warn, >= 500 => StatusTone.Info, _ => StatusTone.Neutral };

    private static string FormatDurationShort(double durationSeconds)
    {
        var rounded = Math.Max(0, (int)Math.Round(durationSeconds));
        var time = TimeSpan.FromSeconds(rounded);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }

    private static string GetHeatmapRangeLabel(HeatmapTimeRange timeRange) => timeRange switch { HeatmapTimeRange.Last24Hours => "24h", HeatmapTimeRange.Last7Days => "7 ngày", HeatmapTimeRange.Last30Days => "30 ngày", _ => "Tất cả" };
    private sealed record HeatmapRangeOption(HeatmapTimeRange Range, string Label);
}
