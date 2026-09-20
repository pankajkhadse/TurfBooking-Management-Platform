namespace TurfBooking.API.Models.Responses.Turf;

public class AvailableSlotResponse
{
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
}