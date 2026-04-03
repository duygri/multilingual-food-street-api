using System.Collections.Generic;

namespace FoodStreet.Client.DTOs
{
    public enum MobileNativeMapMode
    {
        Browse = 0,
        Picker = 1
    }

    public class MobileNativeMapRequest
    {
        public MobileNativeMapMode Mode { get; set; } = MobileNativeMapMode.Browse;
        public string? ScreenTitle { get; set; }
        public double CenterLatitude { get; set; } = 10.7680;
        public double CenterLongitude { get; set; } = 106.7034;
        public float Zoom { get; set; } = 15f;
        public bool HasUserLocation { get; set; }
        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }
        public int? FocusedPoiId { get; set; }
        public List<MobileNativeMapPoiMarker> Pois { get; set; } = new();
    }

    public class MobileNativeMapPoiMarker
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? RadiusMeters { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public double? DistanceMeters { get; set; }
        public bool HasAudio { get; set; }
        public bool IsInGeofence { get; set; }
        public string? ImageUrl { get; set; }
    }
}
