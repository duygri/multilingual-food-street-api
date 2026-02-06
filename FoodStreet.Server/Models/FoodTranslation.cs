namespace PROJECT_C_.Models
{
    public class FoodTranslation
    {
        public int Id { get; set; }
        public int FoodId { get; set; }
        public string LanguageCode { get; set; } = string.Empty; // e.g., "vi-VN", "en-US"
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public Food Food { get; set; } = null!;
    }
}
