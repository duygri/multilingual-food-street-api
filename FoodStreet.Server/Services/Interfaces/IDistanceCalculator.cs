namespace PROJECT_C_.Services.Interfaces
{
    public interface IDistanceCalculator
    {
        double Calculate(double lat1, double lng1, double lat2, double lng2);
    }
}
