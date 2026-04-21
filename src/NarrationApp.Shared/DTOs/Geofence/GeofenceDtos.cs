namespace NarrationApp.Shared.DTOs.Geofence;

public sealed class GeofenceDto
{
    public int Id { get; init; }

    public int PoiId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int RadiusMeters { get; init; }

    public int Priority { get; init; }

    public int DebounceSeconds { get; init; }

    public int CooldownSeconds { get; init; }

    public bool IsActive { get; init; }

    public string TriggerAction { get; init; } = string.Empty;

    public bool NearestOnly { get; init; }
}

public sealed class UpdateGeofenceRequest
{
    public string Name { get; init; } = string.Empty;

    public int RadiusMeters { get; init; }

    public int Priority { get; init; }

    public int DebounceSeconds { get; init; }

    public int CooldownSeconds { get; init; }

    public bool IsActive { get; init; }

    public string TriggerAction { get; init; } = string.Empty;

    public bool NearestOnly { get; init; }
}
