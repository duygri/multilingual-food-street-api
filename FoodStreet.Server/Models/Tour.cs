using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// Tour - Lộ trình tham quan các điểm ẩm thực
    /// </summary>
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Thời gian ước tính (phút)
        public int EstimatedDurationMinutes { get; set; } = 60;

        // Khoảng cách ước tính (km)
        public double EstimatedDistanceKm { get; set; } = 1.0;

        // Trạng thái
        public bool IsActive { get; set; } = true;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<TourItem> Items { get; set; } = new List<TourItem>();
    }
}
