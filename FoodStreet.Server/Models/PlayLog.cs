using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// Log mỗi lần phát audio cho một POI
    /// </summary>
    public class PlayLog
    {
        [Key]
        public int Id { get; set; }

        // POI được nghe
        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }

        // Thời gian
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        public double DurationSeconds { get; set; } // Thời gian nghe thực tế

        // Thiết bị/Session (ẩn danh)
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; } // mobile, desktop
        public string? Language { get; set; }

        // Vị trí (tùy chọn, ẩn danh)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Nguồn: qr_scan, geofence, manual
        public string Source { get; set; } = "manual";
    }
}
