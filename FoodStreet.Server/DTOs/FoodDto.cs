namespace PROJECT_C_.DTOs
{
    public class FoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; }
        public string Unit { get; set; } = "m";

        // POI Extended Fields
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }
        
        // Geofencing
        public double Radius { get; set; } = 50;
        public int Priority { get; set; } = 0;

        // TTS Script
        public string? TtsScript { get; set; }

        // Audio info (for convenience)
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
