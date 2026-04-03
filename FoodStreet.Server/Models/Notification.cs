using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    public enum NotificationType
    {
        POI_Created,      // POI Owner tạo POI mới → thông báo Admin
        POI_Approved,     // Admin duyệt POI → thông báo POI Owner
        POI_Rejected,     // Admin từ chối POI → thông báo POI Owner
        Food_Created,     // Enum legacy: dữ liệu cũ cho sự kiện tạo/cập nhật menu → thông báo Admin
        Food_Approved,    // Enum legacy: dữ liệu cũ cho sự kiện duyệt menu → thông báo POI Owner
        System,           // Thông báo hệ thống
        POI_Updated,      // POI Owner cập nhật POI → cần duyệt lại
        POI_AudioReady,   // Audio / TTS cho POI đã sẵn sàng
        Menu_Created,     // Enum mới: dùng cho sự kiện tạo/cập nhật menu → thông báo Admin
        Menu_Approved     // Enum mới: dùng cho sự kiện duyệt menu → thông báo POI Owner
    }

    public class Notification
    {
        public int Id { get; set; }

        /// <summary>
        /// UserId nhận thông báo (null = tất cả Admin)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Role nhận thông báo ("Admin" hoặc role persistence legacy "Seller" cho POI Owner)
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
        /// ID liên quan (LocationId, MenuItemId, etc.)
        /// </summary>
        public int? RelatedId { get; set; }

        /// <summary>
        /// Tên người gửi (hiển thị UI)
        /// </summary>
        [MaxLength(100)]
        public string? SenderName { get; set; }
    }
}
