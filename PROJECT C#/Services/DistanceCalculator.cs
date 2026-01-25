using PROJECT_C_.Services.Interfaces;

namespace PROJECT_C_.Services
{
    public class DistanceCalculator : IDistanceCalculator
    {
        public double Calculate(double lat1, double lng1, double lat2, double lng2)
        {
            double R = 6371; // km
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double value)
        {
            return value * (Math.PI / 180);
        }
    }
}
