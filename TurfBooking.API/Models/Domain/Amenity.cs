namespace TurfBooking.API.Models.Domain;

public class Amenity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation Property
    public ICollection<TurfAmenity> TurfAmenities { get; set; } = new List<TurfAmenity>();
}