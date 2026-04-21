using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class Tour
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public string? CoverImage { get; set; }

    public TourStatus Status { get; set; }

    public ICollection<TourStop> Stops { get; set; } = [];
}
