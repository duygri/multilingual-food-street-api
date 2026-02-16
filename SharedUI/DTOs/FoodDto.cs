namespace FoodStreet.Client.DTOs
{
    public class FoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Image
        public string? ImageUrl { get; set; }

        // Location reference
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Audio info
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
    }
}
