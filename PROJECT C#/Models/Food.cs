namespace PROJECT_C_.Models
{
    public class Food
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } 
        public string? OwnerId { get; set; } 

        // GPS coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // POI Extended Fields
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }
        
        // Geofencing
        public double Radius { get; set; } = 50; // meters - bán kính kích hoạt
        public int Priority { get; set; } = 0; // Độ ưu tiên (cao hơn = ưu tiên hơn)

        // TTS Script (nếu không có audio file)
        public string? TtsScript { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<FoodTranslation> Translations { get; set; } = new List<FoodTranslation>();
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();
    }
}