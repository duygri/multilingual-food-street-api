using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class VisitEvent
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public int PoiId { get; set; }

    public EventType EventType { get; set; }

    public string Source { get; set; } = string.Empty;

    public int ListenDurationSeconds { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser? User { get; set; }

    public Poi? Poi { get; set; }
}
