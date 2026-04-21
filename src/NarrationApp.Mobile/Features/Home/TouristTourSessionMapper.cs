using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Mobile.Features.Home;

public static class TouristTourSessionMapper
{
    public static TouristTourSession Map(TouristTourCard tour, TourSessionDto session, IReadOnlyList<TouristPoi> pois)
    {
        var isCompleted = session.Status == TourSessionStatus.Completed || session.CurrentStopSequence >= tour.StopPoiIds.Count;
        if (isCompleted)
        {
            return new TouristTourSession(
                TourId: tour.Id,
                TourTitle: tour.Title,
                CurrentStopSequence: session.CurrentStopSequence,
                TotalStops: session.TotalStops,
                NextPoiId: null,
                NextPoiName: "Hoàn thành",
                IsCompleted: true,
                IsServerBacked: true,
                SyncStatus: session.Status);
        }

        var nextStopIndex = Math.Clamp(session.CurrentStopSequence, 0, Math.Max(0, tour.StopPoiIds.Count - 1));
        var nextPoiId = tour.StopPoiIds[nextStopIndex];
        var nextPoiName = pois.FirstOrDefault(poi => poi.Id == nextPoiId)?.Name ?? "POI kế tiếp";

        return new TouristTourSession(
            TourId: tour.Id,
            TourTitle: tour.Title,
            CurrentStopSequence: session.CurrentStopSequence,
            TotalStops: session.TotalStops,
            NextPoiId: nextPoiId,
            NextPoiName: nextPoiName,
            IsCompleted: false,
            IsServerBacked: true,
            SyncStatus: session.Status);
    }
}
