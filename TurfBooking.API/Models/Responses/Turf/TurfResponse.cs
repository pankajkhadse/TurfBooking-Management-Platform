namespace TurfBooking.API.Models.Responses.Turf;

public class TurfResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Locality { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Pincode { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal PricePerSlot { get; set; }

    public int SlotDurationMinutes { get; set; }

    public string Status { get; set; } = string.Empty;
}