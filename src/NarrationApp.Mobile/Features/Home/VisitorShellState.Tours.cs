using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void SelectTour(string tourId)
    {
        if (_tours.Any(tour => tour.Id == tourId))
        {
            SelectedTourId = tourId;
            CurrentTab = VisitorTab.Tours;
            CurrentSettingsScreen = VisitorSettingsScreen.Overview;
        }
    }

    public void StartTour(string tourId)
    {
        var tour = _tours.FirstOrDefault(item => item.Id == tourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            return;
        }

        SelectedTourId = tour.Id;
        var nextPoiId = tour.StopPoiIds[0];
        ActiveTourSession = new VisitorTourSession(
            TourId: tour.Id,
            TourTitle: tour.Title,
            CurrentStopSequence: 0,
            TotalStops: tour.StopPoiIds.Count,
            NextPoiId: nextPoiId,
            NextPoiName: ResolvePoiName(nextPoiId),
            IsCompleted: false);

        OpenPoi(nextPoiId);
    }

    public void ApplyServerTourSession(VisitorTourSession session)
    {
        ActiveTourSession = session;
        SelectedTourId = session.TourId;

        if (!string.IsNullOrWhiteSpace(session.NextPoiId))
        {
            OpenPoi(session.NextPoiId);
            return;
        }

        CurrentTab = VisitorTab.Tours;
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void ClearActiveTourSession()
    {
        ActiveTourSession = null;
    }

    public bool AdvanceActiveTour(string poiId)
    {
        if (ActiveTourSession is null)
        {
            return false;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == ActiveTourSession.TourId);
        if (tour is null || ActiveTourSession.CurrentStopSequence >= tour.StopPoiIds.Count)
        {
            return false;
        }

        var expectedPoiId = tour.StopPoiIds[ActiveTourSession.CurrentStopSequence];
        if (!string.Equals(expectedPoiId, poiId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextSequence = ActiveTourSession.CurrentStopSequence + 1;
        if (nextSequence >= tour.StopPoiIds.Count)
        {
            ActiveTourSession = ActiveTourSession with
            {
                CurrentStopSequence = nextSequence,
                NextPoiId = null,
                NextPoiName = "Hoàn thành",
                IsCompleted = true
            };

            return true;
        }

        var nextPoiId = tour.StopPoiIds[nextSequence];
        ActiveTourSession = ActiveTourSession with
        {
            CurrentStopSequence = nextSequence,
            NextPoiId = nextPoiId,
            NextPoiName = ResolvePoiName(nextPoiId),
            IsCompleted = false
        };

        OpenPoi(nextPoiId);
        return true;
    }

    private void EnsureSelectedTourStillVisible()
    {
        if (_tours.Count == 0)
        {
            SelectedTourId = null;
            return;
        }

        if (SelectedTourId is not null && _tours.Any(tour => tour.Id == SelectedTourId))
        {
            return;
        }

        SelectedTourId = _tours[0].Id;
    }

    private void EnsureActiveTourStillVisible()
    {
        if (ActiveTourSession is null)
        {
            return;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == ActiveTourSession.TourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            ActiveTourSession = null;
            return;
        }

        if (ActiveTourSession.IsCompleted)
        {
            return;
        }

        var nextIndex = Math.Clamp(ActiveTourSession.CurrentStopSequence, 0, tour.StopPoiIds.Count - 1);
        var nextPoiId = tour.StopPoiIds[nextIndex];
        ActiveTourSession = ActiveTourSession with
        {
            TotalStops = tour.StopPoiIds.Count,
            NextPoiId = nextPoiId,
            NextPoiName = ResolvePoiName(nextPoiId)
        };
    }

    private void ApplyPendingQrNavigationTargetIfReady()
    {
        if (_pendingQrNavigationTarget is null)
        {
            return;
        }

        if (TryApplyQrNavigationTarget(_pendingQrNavigationTarget))
        {
            _pendingQrNavigationTarget = null;
        }
    }

    private bool TryApplyQrNavigationTarget(VisitorQrNavigationTarget target)
    {
        switch (target.Kind)
        {
            case VisitorQrTargetKind.OpenApp:
                SwitchTab(VisitorTab.Map);
                return true;

            case VisitorQrTargetKind.PoiList:
                ResetDiscoverFilters();
                SelectedPoiId = null;
                ShowPoiSheet = false;
                ShowMiniPlayer = false;
                SwitchTab(VisitorTab.Discover);
                return true;

            case VisitorQrTargetKind.Poi when !string.IsNullOrWhiteSpace(target.TargetId) && _pois.Any(poi => poi.Id == target.TargetId):
                OpenPoi(target.TargetId);
                return true;

            case VisitorQrTargetKind.Tour when IsTourReadyForQrStart(target.TargetId):
                StartTour(target.TargetId!);
                return true;

            default:
                return false;
        }
    }

    private bool IsTourReadyForQrStart(string? tourId)
    {
        if (string.IsNullOrWhiteSpace(tourId))
        {
            return false;
        }

        var tour = _tours.FirstOrDefault(item => item.Id == tourId);
        if (tour is null || tour.StopPoiIds.Count == 0)
        {
            return false;
        }

        var firstStopPoiId = tour.StopPoiIds[0];
        return _pois.Any(poi => poi.Id == firstStopPoiId);
    }
}
