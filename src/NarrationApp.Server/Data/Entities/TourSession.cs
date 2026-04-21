using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class TourSession
{
    public int Id { get; set; }

    public int TourId { get; set; }

    public Guid UserId { get; set; }

    public TourSessionStatus Status { get; set; }

    public int CurrentStopSequence { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Tour? Tour { get; set; }

    public AppUser? User { get; set; }
}
