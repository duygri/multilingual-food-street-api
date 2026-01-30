using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECT_C_.Models
{
    public class AudioFile
    {
        [Key]
        public int Id { get; set; }

        public required string FileName { get; set; }
        public string? OriginalName { get; set; }
        public string ContentType { get; set; } = "audio/mpeg";
        public long Size { get; set; }
        public double DurationSeconds { get; set; }

        // Relationship to Food/POI
        public int? FoodId { get; set; }
        [ForeignKey("FoodId")]
        [System.Text.Json.Serialization.JsonIgnore]
        public Food? Food { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
