namespace PROJECT_C_.Models
{
    public class Food
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Ảnh riêng của món ăn
        public string? ImageUrl { get; set; }

        // Thuộc về Location (địa điểm)
        public int? LocationId { get; set; }
        public Location? Location { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<FoodTranslation> Translations { get; set; } = new List<FoodTranslation>();
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();
    }
}