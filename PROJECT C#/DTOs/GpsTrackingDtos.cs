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
    }

    public class GeofenceCheckResponse
    {
        public List<NearbyPoiResponse> EnteredPois { get; set; } = new();
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
    }
}
