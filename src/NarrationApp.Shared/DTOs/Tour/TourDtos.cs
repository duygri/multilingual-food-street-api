using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Tour;

public sealed class TourDto
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int EstimatedMinutes { get; init; }

    public string? CoverImage { get; init; }

    public TourStatus Status { get; init; }

    public IReadOnlyList<TourStopDto> Stops { get; init; } = Array.Empty<TourStopDto>();
}

public sealed class CreateTourRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int EstimatedMinutes { get; init; }

    public string? CoverImage { get; init; }

    public IReadOnlyList<UpsertTourStopRequest> Stops { get; init; } = Array.Empty<UpsertTourStopRequest>();
}

public sealed class UpdateTourRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int EstimatedMinutes { get; init; }

    public string? CoverImage { get; init; }

    public TourStatus Status { get; init; }

    public IReadOnlyList<UpsertTourStopRequest> Stops { get; init; } = Array.Empty<UpsertTourStopRequest>();
}

public sealed class UpsertTourStopRequest
{
    public int PoiId { get; init; }

    public int Sequence { get; init; }

    public int RadiusMeters { get; init; }
}

public sealed class TourStopDto
{
    public int Id { get; init; }

    public int TourId { get; init; }

    public int PoiId { get; init; }

    public int Sequence { get; init; }

    public int RadiusMeters { get; init; }
}

public sealed class UpdateTourProgressRequest
{
    public int PoiId { get; init; }

    public string? DeviceId { get; init; }

    public double? Lat { get; init; }

    public double? Lng { get; init; }
}

public sealed class TourSessionDto
{
    public int Id { get; init; }

    public int TourId { get; init; }

    public Guid UserId { get; init; }

    public TourSessionStatus Status { get; init; }

    public int CurrentStopSequence { get; init; }

    public int TotalStops { get; init; }

    public DateTime StartedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
}
