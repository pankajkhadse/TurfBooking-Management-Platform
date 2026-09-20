using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Requests.Turf;

public class UpdateTurfAmenitiesRequest
{
    public List<AmenityType> Amenities { get; set; } = new();
}