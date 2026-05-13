using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Server.Services;

public sealed partial class TourService
{
    private static TourDto MapTour(Tour tour)
    {
        return new TourDto
        {
            Id = tour.Id,
            Title = tour.Title,
            Description = tour.Description,
            EstimatedMinutes = tour.EstimatedMinutes,
            CoverImage = tour.CoverImage,
            Status = tour.Status,
            Stops = tour.Stops
                .OrderBy(item => item.Sequence)
                .Select(MapStop)
                .ToArray()
        };
    }

    private static TourStopDto MapStop(TourStop stop)
    {
        return new TourStopDto
        {
            Id = stop.Id,
            TourId = stop.TourId,
            PoiId = stop.PoiId,
            Sequence = stop.Sequence,
            RadiusMeters = stop.RadiusMeters
        };
    }

    private static TourSessionDto MapSession(TourSession session, int totalStops)
    {
        return new TourSessionDto
        {
            Id = session.Id,
            TourId = session.TourId,
            UserId = session.UserId,
            Status = session.Status,
            CurrentStopSequence = session.CurrentStopSequence,
            TotalStops = totalStops,
            StartedAtUtc = session.StartedAt,
            UpdatedAtUtc = session.UpdatedAt,
            CompletedAtUtc = session.CompletedAt
        };
    }
}
