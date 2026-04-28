using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class Analytics : IAsyncDisposable
{
    private const string HeatmapContainerId = "analytics-heatmap-map";
    private const string MovementContainerId = "analytics-flow-map";

    private bool _isLoading = true;
    private bool _isHeatmapLoading;
    private bool _isMovementFlowLoading;
    private bool _pendingMapRender;
    private string? _errorMessage;
    private string? _heatmapErrorMessage;
    private string? _movementFlowErrorMessage;
    private DashboardDto _overview = new();
    private AnalyticsSnapshotDto _snapshot = new();
    private IReadOnlyList<TopPoiDto> _topPois = Array.Empty<TopPoiDto>();
    private IReadOnlyList<HeatmapPointDto> _heatmap = Array.Empty<HeatmapPointDto>();
    private IReadOnlyList<MovementFlowDto> _movementFlows = Array.Empty<MovementFlowDto>();
    private IReadOnlyList<PoiAverageListenDto> _averageListenByPoi = Array.Empty<PoiAverageListenDto>();
    private HeatmapTimeRange _selectedHeatmapTimeRange = HeatmapTimeRange.Last7Days;
    private EventType? _selectedHeatmapEventType;
    private bool _useHeatmapDecay = true;
    private HeatmapTimeRange _selectedMovementFlowTimeRange = HeatmapTimeRange.Last7Days;
    private EventType? _selectedMovementFlowEventType;
    private int _minimumMovementFlowSessions = 3;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _overview = await AdminPortalService.GetOverviewAsync();
            _snapshot = await AdminPortalService.GetAnalyticsSnapshotAsync();
            await LoadMovementFlowsAsync();
            _topPois = await AdminPortalService.GetTopPoisAsync();
            _averageListenByPoi = await AdminPortalService.GetAverageListenByPoiAsync();
            await LoadHeatmapAsync();
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
