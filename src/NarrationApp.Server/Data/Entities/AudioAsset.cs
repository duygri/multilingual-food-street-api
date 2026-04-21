using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Entities;

public sealed class AudioAsset
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public AudioSourceType SourceType { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public AudioStatus Status { get; set; }

    public int DurationSeconds { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public Poi? Poi { get; set; }
}
