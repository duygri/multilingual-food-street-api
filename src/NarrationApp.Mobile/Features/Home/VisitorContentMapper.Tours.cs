using NarrationApp.Shared.DTOs.Tour;

namespace NarrationApp.Mobile.Features.Home;

public static partial class VisitorContentMapper
{
    private static VisitorTourCard MapTour(TourDto tour)
    {
        return new VisitorTourCard(
            Id: $"tour-{tour.Id}",
            Title: tour.Title,
            StopCountLabel: $"{tour.Stops.Count} điểm dừng",
            DurationLabel: $"{tour.EstimatedMinutes} phút",
            DifficultyLabel: GetDifficultyLabel(tour.Stops.Count, tour.EstimatedMinutes),
            Description: string.IsNullOrWhiteSpace(tour.Description) ? "Tour sẵn sàng để bắt đầu." : tour.Description,
            StopPoiIds: tour.Stops
                .OrderBy(stop => stop.Sequence)
                .Select(stop => $"poi-{stop.PoiId}")
                .ToArray());
    }

    private static string GetDifficultyLabel(int stopCount, int estimatedMinutes)
    {
        if (estimatedMinutes <= 30 || stopCount <= 3)
        {
            return "Nhanh";
        }

        if (estimatedMinutes <= 45 || stopCount <= 5)
        {
            return "Dễ đi bộ";
        }

        return "Khám phá";
    }
}
