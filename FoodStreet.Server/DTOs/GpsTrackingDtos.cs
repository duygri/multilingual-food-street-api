namespace PROJECT_C_.DTOs
{
    public class UpdateLocationRequest
    {
        public required string SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? Speed { get; set; }
    }

    public class NearbyPoiResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Distance { get; set; } // meters
        public double Radius { get; set; }
        public bool IsInGeofence { get; set; }
        public string? ImageUrl { get; set; }
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
        public string AudioStatus { get; set; } = "pending";
        public string LanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; } = 3;
        public bool FallbackUsed { get; set; }
        public string? TtsScript { get; set; }
        public bool IsFallback { get; set; }
        public int Priority { get; set; }
    }

    public class GeofenceCheckResponse
    {
        public List<NearbyPoiResponse> EnteredPois { get; set; } = new();
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
    }
}
