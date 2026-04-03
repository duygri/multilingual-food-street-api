namespace PROJECT_C_.Models
{
    public class PoiMenuItemTranslation
    {
        public int Id { get; set; }
        public int PoiMenuItemId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFallback { get; set; }
        public DateTime? GeneratedAt { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public PoiMenuItem PoiMenuItem { get; set; } = null!;
    }
}
