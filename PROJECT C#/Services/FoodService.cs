using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using System.Xml.Linq;

public class FoodService : IFoodService
{
    private readonly List<Food> _foods = new()
    {
        new Food
        {
            Id = 1,
            Name = "Banh mi",
            Description = "Banh mi",
            Latitude = 10.762622,
            Longitude  = 106.660172
        },
        new Food
        {
            Id = 2,
            Name = "Pho",
            Description = "Pho",
            Latitude = 10.776889,
            Longitude = 106.700806
        }
    };
    
    public List<FoodDto> GetNearestFoods(double lat, double lng, int top)
    {
        return _foods
            .Select(f =>
            {
                var km = CalculateDistance(lat, lng, f.Latitude, f.Longitude);
                return new 
                {
                    Food = f,
                    Km = km
                };
            })
            .OrderBy(x => x.Km)
            .Take(top)
            .Select(x => new FoodDto
            {
                Id = x.Food.Id,
                Name = x.Food.Name,
                Description = x.Food.Description,
                Distance = x.Km < 1 ? Math.Round(x.Km * 1000, 0) : Math.Round(x.Km, 2),
                Unit = x.Km < 1 ? "m" : "km"
            })
            .ToList();
    }
    
    public double CalculateDistance(
        double lat1, double lng1,
        double lat2, double lng2)
    {
        double R = 6371; // Radius of the earth in km
        double dLat = ToRadians(lat2 - lat1);
        double dLng = ToRadians(lng2 - lng1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c; // Distance in km
        return distance;
    }
    
    private double ToRadians(double value)
    {
        return value * (Math.PI / 180);
    }
}
