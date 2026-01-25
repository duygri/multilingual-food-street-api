namespace PROJECT_C_.DTOs
{
    public class FoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double Distance { get; set; }
        public string Unit { get; set; } = "m";
    }
}
