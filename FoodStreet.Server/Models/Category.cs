namespace PROJECT_C_.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Icon { get; set; } // Emoji hoặc URL icon
        public string? Description { get; set; }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Food> Foods { get; set; } = new List<Food>();
    }
}
