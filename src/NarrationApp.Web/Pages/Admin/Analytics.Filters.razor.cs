using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class Analytics
{
    private async Task ChangeHeatmapTimeRangeAsync(HeatmapTimeRange timeRange)
    {
        if (_selectedHeatmapTimeRange == timeRange && !_isHeatmapLoading) return;
        _selectedHeatmapTimeRange = timeRange;
        await LoadHeatmapAsync();
    }

    private async Task ChangeHeatmapEventTypeAsync(EventType? eventType)
    {
        if (_selectedHeatmapEventType == eventType && !_isHeatmapLoading) return;
        _selectedHeatmapEventType = eventType;
        await LoadHeatmapAsync();
    }

    private async Task ToggleHeatmapDecayAsync()
    {
        _useHeatmapDecay = !_useHeatmapDecay;
        await LoadHeatmapAsync();
    }

    private async Task ChangeMovementFlowTimeRangeAsync(HeatmapTimeRange timeRange)
    {
        if (_selectedMovementFlowTimeRange == timeRange && !_isMovementFlowLoading) return;
        _selectedMovementFlowTimeRange = timeRange;
        await LoadMovementFlowsAsync();
    }

    private async Task ChangeMovementFlowEventTypeAsync(EventType? eventType)
    {
        if (_selectedMovementFlowEventType == eventType && !_isMovementFlowLoading) return;
        _selectedMovementFlowEventType = eventType;
        await LoadMovementFlowsAsync();
    }

    private async Task ChangeMovementFlowMinimumSessionsAsync(int minimumUniqueSessions)
    {
        if (_minimumMovementFlowSessions == minimumUniqueSessions && !_isMovementFlowLoading) return;
        _minimumMovementFlowSessions = minimumUniqueSessions;
        await LoadMovementFlowsAsync();
    }

    private async Task LoadHeatmapAsync()
    {
        _isHeatmapLoading = true;
        _heatmapErrorMessage = null;

        try
        {
            _heatmap = await AdminPortalService.GetHeatmapAsync(new HeatmapQueryDto
            {
                TimeRange = _selectedHeatmapTimeRange,
                EventTypeFilter = _selectedHeatmapEventType,
                UseTimeDecay = _useHeatmapDecay,
                GridSizeMeters = 50d,
                MaxWeight = 50d,
                ApplyGaussianSmoothing = true
            });
        }
        catch (ApiException exception)
        {
            _heatmap = Array.Empty<HeatmapPointDto>();
            _heatmapErrorMessage = exception.Message;
        }
        finally
        {
            _isHeatmapLoading = false;
            _pendingMapRender = true;
        }
    }

    private async Task LoadMovementFlowsAsync()
    {
        _isMovementFlowLoading = true;
        _movementFlowErrorMessage = null;

        try
        {
            _movementFlows = await AdminPortalService.GetMovementFlowsAsync(new MovementFlowQueryDto
            {
                TimeRange = _selectedMovementFlowTimeRange,
                EventTypeFilter = _selectedMovementFlowEventType,
                MinimumUniqueSessions = _minimumMovementFlowSessions
            });
        }
        catch (ApiException exception)
        {
            _movementFlows = Array.Empty<MovementFlowDto>();
            _movementFlowErrorMessage = exception.Message;
        }
        finally
        {
            _isMovementFlowLoading = false;
            _pendingMapRender = true;
        }
    }
}
