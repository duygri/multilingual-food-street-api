using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// Persisted tour session for authenticated tourists so an in-progress journey
    /// can be resumed across page reloads and app restarts.
    /// </summary>
    public class TourSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        public int TourId { get; set; }
        public int CurrentLocationId { get; set; }
        public int CurrentStopOrder { get; set; }
        public int CompletedStops { get; set; }
        public int TotalStops { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsCompleted { get; set; }
        public int ResumeCount { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        public double? LastLatitude { get; set; }
        public double? LastLongitude { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastResumedAt { get; set; }
        public DateTime? DismissedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public Tour? Tour { get; set; }
    }
}
