using System.ComponentModel.DataAnnotations;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// Lưu trữ Web Push subscription của user
    /// </summary>
    public class PushSubscription
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Session ID để liên kết với user (ẩn danh)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Push subscription endpoint URL
        /// </summary>
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// P256DH key (Base64)
        /// </summary>
        [Required]
        public string P256dh { get; set; } = string.Empty;

        /// <summary>
        /// Auth key (Base64)
        /// </summary>
        [Required]
        public string Auth { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm đăng ký
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Có active không
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Ngôn ngữ ưu tiên cho notifications
        /// </summary>
        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "vi";
    }
}
