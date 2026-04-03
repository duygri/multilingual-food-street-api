namespace PROJECT_C_.Models
{
    public class PoiMenuItem
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [System.Text.Json.Serialization.JsonIgnore]
        public Location Location { get; set; } = null!;

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<PoiMenuItemTranslation> Translations { get; set; } = new List<PoiMenuItemTranslation>();
    }
}
