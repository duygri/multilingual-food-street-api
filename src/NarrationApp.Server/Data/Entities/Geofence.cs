namespace NarrationApp.Server.Data.Entities;

public sealed class Geofence
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RadiusMeters { get; set; }

    public int Priority { get; set; }

    public int DebounceSeconds { get; set; }

    public int CooldownSeconds { get; set; }

    public bool IsActive { get; set; }

    public string TriggerAction { get; set; } = string.Empty;

    public bool NearestOnly { get; set; }

    public Poi? Poi { get; set; }
}
