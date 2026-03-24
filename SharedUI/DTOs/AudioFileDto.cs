namespace FoodStreet.Client.DTOs
{
    public class AudioFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? OriginalName { get; set; }
        public string ContentType { get; set; } = "audio/mpeg";
        public long Size { get; set; }
        public double DurationSeconds { get; set; }
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class AudioStatsDto
    {
        public int TotalFiles { get; set; }
        public int Assigned { get; set; }
        public int Unassigned { get; set; }
        public double TotalSizeMB { get; set; }
    }
}
