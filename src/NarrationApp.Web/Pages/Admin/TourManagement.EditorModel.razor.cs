using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class TourManagement
{
    private sealed class TourEditorModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; } = 30;
        public string? CoverImage { get; set; }
        public TourStatus Status { get; set; } = TourStatus.Draft;
        public List<TourStopEditorModel> Stops { get; set; } = [new()];

        public static TourEditorModel CreateDefault() => new();

        public static TourEditorModel FromTour(TourDto tour) => new()
        {
            Title = tour.Title,
            Description = tour.Description,
            EstimatedMinutes = tour.EstimatedMinutes,
            CoverImage = tour.CoverImage,
            Status = tour.Status,
            Stops = tour.Stops.OrderBy(item => item.Sequence)
                .Select(item => new TourStopEditorModel { PoiId = item.PoiId, RadiusMeters = item.RadiusMeters })
                .ToList()
        };

        public CreateTourRequest ToCreateRequest() => new()
        {
            Title = Title.Trim(),
            Description = Description.Trim(),
            EstimatedMinutes = EstimatedMinutes,
            CoverImage = string.IsNullOrWhiteSpace(CoverImage) ? null : CoverImage.Trim(),
            Stops = Stops.Select((item, index) => new UpsertTourStopRequest { PoiId = item.PoiId, Sequence = index + 1, RadiusMeters = item.RadiusMeters }).ToArray()
        };

        public UpdateTourRequest ToUpdateRequest() => new()
        {
            Title = Title.Trim(),
            Description = Description.Trim(),
            EstimatedMinutes = EstimatedMinutes,
            CoverImage = string.IsNullOrWhiteSpace(CoverImage) ? null : CoverImage.Trim(),
            Status = Status,
            Stops = Stops.Select((item, index) => new UpsertTourStopRequest { PoiId = item.PoiId, Sequence = index + 1, RadiusMeters = item.RadiusMeters }).ToArray()
        };
    }

    private sealed class TourStopEditorModel
    {
        public int PoiId { get; set; }
        public int RadiusMeters { get; set; } = 60;
    }
}
