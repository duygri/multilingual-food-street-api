using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// Lưu vị trí GPS người dùng (ẩn danh qua SessionId)
    /// </summary>
    public class UserLocation
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Session ID ẩn danh để track user mà không cần login
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        /// <summary>
        /// Độ chính xác GPS (meters)
        /// </summary>
        public double? Accuracy { get; set; }

        /// <summary>
        /// Tốc độ di chuyển (m/s) - dùng cho adaptive polling
        /// </summary>
        public double? Speed { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
