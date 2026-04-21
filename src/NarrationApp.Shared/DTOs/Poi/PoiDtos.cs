using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Shared.DTOs.Poi;

public sealed class PoiDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public Guid OwnerId { get; init; }

    public double Lat { get; init; }

    public double Lng { get; init; }

    public int Priority { get; init; }

    public int? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public NarrationMode NarrationMode { get; init; }

    public string Description { get; init; } = string.Empty;

    public string TtsScript { get; init; } = string.Empty;

    public string? MapLink { get; init; }

    public string? ImageUrl { get; init; }

    public PoiStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public IReadOnlyList<TranslationDto> Translations { get; init; } = Array.Empty<TranslationDto>();

    public IReadOnlyList<GeofenceDto> Geofences { get; init; } = Array.Empty<GeofenceDto>();
}

public sealed class CreatePoiRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public double Lat { get; init; }

    public double Lng { get; init; }

    public int Priority { get; init; }

    public int? CategoryId { get; init; }

    public NarrationMode NarrationMode { get; init; }

    public string Description { get; init; } = string.Empty;

    public string TtsScript { get; init; } = string.Empty;

    public string? MapLink { get; init; }

    public string? ImageUrl { get; init; }
}

public sealed class UpdatePoiRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public double Lat { get; init; }

    public double Lng { get; init; }

    public int Priority { get; init; }

    public int? CategoryId { get; init; }

    public NarrationMode NarrationMode { get; init; }

    public string Description { get; init; } = string.Empty;

    public string TtsScript { get; init; } = string.Empty;

    public string? MapLink { get; init; }

    public string? ImageUrl { get; init; }

    public PoiStatus Status { get; init; }
}

public sealed class PoiNearRequest
{
    public double Lat { get; init; }

    public double Lng { get; init; }

    public int RadiusMeters { get; init; }
}
