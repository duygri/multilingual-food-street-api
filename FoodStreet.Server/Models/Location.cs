namespace PROJECT_C_.Models
{
    /// <summary>
    /// Địa điểm / POI - do Seller tạo, Admin duyệt.
    /// </summary>
    public class Location
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Address { get; set; }

        // GPS coordinates (POI)
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Geofencing
        public double Radius { get; set; } = 50; // meters - bán kính kích hoạt
        public int Priority { get; set; } = 0;    // Độ ưu tiên (cao hơn = ưu tiên hơn)

        // Media
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }

        // TTS Script (nếu không có audio file)
        public string? TtsScript { get; set; }

        // Ownership (Seller)
        public string? OwnerId { get; set; }

        // Approval Workflow
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Navigation properties

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<LocationTranslation> Translations { get; set; } = new List<LocationTranslation>();

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();
    }
}
