using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void UpdateLocation(VisitorLocationSnapshot location)
    {
        CurrentLocation = location;
        LocationPermissionGranted = location.PermissionGranted;
        LocationStatusLabel = VisitorLocationStatusFormatter.Build(location);

        var projectedPois = VisitorPoiDistanceProjector.Apply(_pois, location);
        var orderedPois = VisitorPoiPresentationOrder.Apply(
            projectedPois,
            VisitorGeoMath.TryGetCoordinates(location, out _, out _));

        _pois.Clear();
        _pois.AddRange(orderedPois);
        InvalidatePoiViews();
    }

    public void ApplyProximityFocus(VisitorProximityMatch? proximity)
    {
        var previousProximityPoiId = ActiveProximity?.PoiId;
        ActiveProximity = proximity;

        if (proximity is null)
        {
            AutoNarrationPrompt = "Chưa ở trong vùng phát tự động.";
            _dismissedProximityPoiId = null;
            return;
        }

        AutoNarrationPrompt = $"Bạn đang ở gần {proximity.PoiName} ({proximity.DistanceMeters}m). Sẵn sàng phát audio tự động.";

        var enteredDifferentPoi = !string.Equals(previousProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase);
        if (enteredDifferentPoi && !string.Equals(_dismissedProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = null;
        }

        if (string.Equals(_dismissedProximityPoiId, proximity.PoiId, StringComparison.OrdinalIgnoreCase))
        {
            PreviewPoi(proximity.PoiId);
            return;
        }

        OpenPoi(proximity.PoiId);

        if (enteredDifferentPoi)
        {
            AddProximityNotification(proximity);
        }
    }
}
