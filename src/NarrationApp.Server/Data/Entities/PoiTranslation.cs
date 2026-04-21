namespace NarrationApp.Server.Data.Entities;

public sealed class PoiTranslation
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Story { get; set; } = string.Empty;

    public string Highlight { get; set; } = string.Empty;

    public bool IsFallback { get; set; }

    public Poi? Poi { get; set; }
}
