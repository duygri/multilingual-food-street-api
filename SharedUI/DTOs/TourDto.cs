namespace FoodStreet.Client.DTOs
{
    public class TourDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public double EstimatedDistanceKm { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ItemCount { get; set; }
        public List<TourStopDto> Items { get; set; } = new();
    }

    public class TourStopDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? LocationDescription { get; set; }
        public string? LocationImageUrl { get; set; }
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        public int Order { get; set; }
        public string? Note { get; set; }
        public int EstimatedStopMinutes { get; set; }
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
        public string AudioStatus { get; set; } = "pending";
        public string LanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; } = 3;
        public bool FallbackUsed { get; set; }
        public bool IsFallback { get; set; }
        public string? DeepLink { get; set; }
    }

    public class TourSessionDto
    {
        public int TourId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
        public int ProgressPercent { get; set; }
        public TourStopDto? CurrentStop { get; set; }
        public TourStopDto? NextStop { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class StartTourRequestDto
    {
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class TourProgressRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int CurrentLocationId { get; set; }
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ResumeTourRequestDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class TourResumeSnapshotDto
    {
        public int TourId { get; set; }
        public string TourName { get; set; } = string.Empty;
        public string? TourDescription { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int StopOrder { get; set; }
        public string CurrentStopName { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public int CompletedStops { get; set; }
        public int TotalStops { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
