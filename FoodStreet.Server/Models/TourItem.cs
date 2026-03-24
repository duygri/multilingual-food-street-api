using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECT_C_.Models
{
    /// <summary>
    /// TourItem - Mỗi điểm dừng trong Tour
    /// </summary>
    public class TourItem
    {
        [Key]
        public int Id { get; set; }

        // Tour chứa item này
        public int TourId { get; set; }
        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        // POI tại điểm dừng
        public int LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }

        // Thứ tự trong tour (1, 2, 3...)
        public int Order { get; set; }

        // Ghi chú cho điểm dừng này
        [MaxLength(500)]
        public string? Note { get; set; }

        // Thời gian dừng ước tính (phút)
        public int EstimatedStopMinutes { get; set; } = 15;
    }
}
