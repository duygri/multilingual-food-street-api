namespace FoodStreet.Client.DTOs
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
        public int Priority { get; set; }

        // Media
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }
        public string? TtsScript { get; set; }

        // Ownership / Approval
        public string? OwnerId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Computed fields (from API)
        public double Distance { get; set; }
        public string Unit { get; set; } = "m";
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
        public int FoodCount { get; set; }
    }
}
