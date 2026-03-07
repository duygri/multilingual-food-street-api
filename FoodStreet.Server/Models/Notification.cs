using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    public enum NotificationType
    {
        POI_Created,      // Seller tạo POI mới → thông báo Admin
        POI_Approved,     // Admin duyệt POI → thông báo Seller
        POI_Rejected,     // Admin từ chối POI → thông báo Seller
        Food_Created,     // Seller tạo món ăn → thông báo Admin
        Food_Approved,    // Admin duyệt món ăn → thông báo Seller
        System            // Thông báo hệ thống
    }

    public class Notification
    {
        public int Id { get; set; }

        /// <summary>
        /// UserId nhận thông báo (null = tất cả Admin)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Role nhận thông báo ("Admin" hoặc "Seller")
        /// Nếu UserId null → gửi cho tất cả user có role này
        /// </summary>
        [MaxLength(20)]
        public string? TargetRole { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID liên quan (LocationId, FoodId, etc.)
        /// </summary>
        public int? RelatedId { get; set; }

        /// <summary>
        /// Tên người gửi (hiển thị UI)
        /// </summary>
        [MaxLength(100)]
        public string? SenderName { get; set; }
    }
}
