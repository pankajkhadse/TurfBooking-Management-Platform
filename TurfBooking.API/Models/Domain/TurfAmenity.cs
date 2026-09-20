using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Domain;

public class TurfAmenity
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public int AmenityId { get; set; }

    public Turf Turf { get; set; } = null!;

    public AmenityType Amenity { get; set; }
}

