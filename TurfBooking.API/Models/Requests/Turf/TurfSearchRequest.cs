namespace TurfBooking.API.Models.Requests.Turf;

public class TurfSearchRequest
{
    // User's current location - optional
    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    // Default 10 km when location is provided
    public double RadiusKm { get; set; } = 10;

    // Example: Football, Cricket
    public string? Sport { get; set; }

    // Maximum price per slot
    public decimal? MaxPrice { get; set; }

    public string? City { get; set; }

    public string? Locality { get; set; }

    // distance / price
    public string SortBy { get; set; } = "distance";
}