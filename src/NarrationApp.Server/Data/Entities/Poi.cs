using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class Poi
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public int Priority { get; set; }

    public int? CategoryId { get; set; }

    public NarrationMode NarrationMode { get; set; }

    public string Description { get; set; } = string.Empty;

    public string TtsScript { get; set; } = string.Empty;

    public string? MapLink { get; set; }

    public string? ImageUrl { get; set; }

    public PoiStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser? Owner { get; set; }

    public Category? Category { get; set; }

    public ICollection<PoiTranslation> Translations { get; set; } = [];

    public ICollection<AudioAsset> AudioAssets { get; set; } = [];

    public ICollection<Geofence> Geofences { get; set; } = [];

    public ICollection<TourStop> TourStops { get; set; } = [];

    public ICollection<VisitEvent> VisitEvents { get; set; } = [];
}
