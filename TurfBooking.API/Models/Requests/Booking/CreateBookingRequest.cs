namespace TurfBooking.API.Models.Requests.Booking;

public class CreateBookingRequest
{
    public int TurfId { get; set; }

    public DateTime BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}