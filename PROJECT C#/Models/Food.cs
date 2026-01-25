namespace PROJECT_C_.Models
{
    public class Food
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } 

        // GPS coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}