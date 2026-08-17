namespace backend.Services.Interfaces
{
    public interface IDistanceService
    {
        double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
    }
}
