using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class PoiReview
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public PoiReviewStatus Status { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public Poi? Poi { get; set; }
}
