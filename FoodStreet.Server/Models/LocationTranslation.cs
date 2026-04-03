namespace PROJECT_C_.Models
{
    public class LocationTranslation
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi-VN", "en-US"
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 3-Tier Content Fallback fields
        public string? TtsScript { get; set; }           // Script for TTS generation
        public string? AudioUrl { get; set; }             // URL to generated audio file (null = no audio)
        public bool IsFallback { get; set; } = false;     // Tier 2: English fallback marker
        public DateTime? GeneratedAt { get; set; }        // Timestamp for cache-busting (?v={mtime})

        [System.Text.Json.Serialization.JsonIgnore]
        public Location Location { get; set; } = null!;
    }
}
