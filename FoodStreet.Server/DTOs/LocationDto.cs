namespace PROJECT_C_.DTOs
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Address { get; set; }

        // GPS
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Geofencing
        public double Radius { get; set; } = 50;
        public int Priority { get; set; } = 0;

        // Media
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }

        // TTS Script
        public string? TtsScript { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Approval (read-only for POI Owner)
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? OwnerId { get; set; }

        // Computed fields (GPS result)
        public double Distance { get; set; }
        public string Unit { get; set; } = "m";

        // Audio info
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
        public string AudioStatus { get; set; } = "pending";
        public string LanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; } = 3;
        public bool FallbackUsed { get; set; }
        public bool IsFallback { get; set; }

    }
}
