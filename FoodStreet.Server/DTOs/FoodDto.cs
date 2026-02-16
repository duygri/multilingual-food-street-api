namespace PROJECT_C_.DTOs
{
    public class FoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Ảnh riêng của món ăn
        public string? ImageUrl { get; set; }

        // Thuộc về Location
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
