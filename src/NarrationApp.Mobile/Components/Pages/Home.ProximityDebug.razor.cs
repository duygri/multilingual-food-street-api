using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private readonly List<VisitorDebugEvent> _geofenceDebugEvents = [];

    private IReadOnlyList<VisitorDebugEvent> GeofenceDebugEvents => _geofenceDebugEvents;

    private void TrackProximityQueueTransition(
        VisitorProximityQueueState previousState,
        VisitorProximityQueueState nextState)
    {
        if (!SamePoi(previousState.ActiveMatch, nextState.ActiveMatch))
        {
            if (previousState.ActiveMatch is not null && nextState.ActiveMatch is null)
            {
                AppendGeofenceDebugEvent(VisitorProximityDebugEventFormatter.BuildExited(previousState.ActiveMatch));
            }
            else if (previousState.ActiveMatch is null && nextState.ActiveMatch is not null)
            {
                AppendGeofenceDebugEvent(VisitorProximityDebugEventFormatter.BuildActive(nextState.ActiveMatch));
            }
            else if (nextState.ActiveMatch is not null)
            {
                AppendGeofenceDebugEvent(VisitorProximityDebugEventFormatter.BuildPromoted(nextState.ActiveMatch));
            }
        }

        if (!SamePoi(previousState.QueuedMatch, nextState.QueuedMatch)
            && nextState.QueuedMatch is not null
            && !SamePoi(nextState.ActiveMatch, nextState.QueuedMatch))
        {
            AppendGeofenceDebugEvent(VisitorProximityDebugEventFormatter.BuildQueued(nextState.QueuedMatch));
        }
    }

    private void AppendGeofenceDebugEvent(string message)
    {
        _geofenceDebugEvents.Insert(0, new VisitorDebugEvent(DateTimeOffset.Now.ToString("HH:mm:ss"), message));
        if (_geofenceDebugEvents.Count > 8)
        {
            _geofenceDebugEvents.RemoveRange(8, _geofenceDebugEvents.Count - 8);
        }

        VisitorMobileDiagnostics.Log("Geofence", message);
    }

    private static bool SamePoi(VisitorProximityMatch? left, VisitorProximityMatch? right) =>
        left is not null
        && right is not null
        && string.Equals(left.PoiId, right.PoiId, StringComparison.OrdinalIgnoreCase);
}
