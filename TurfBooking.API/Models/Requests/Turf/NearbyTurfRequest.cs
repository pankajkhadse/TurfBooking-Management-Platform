namespace TurfBooking.API.Models.Requests.Turf;

public class NearbyTurfRequest
{
    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public double RadiusKm { get; set; } = 10;
}