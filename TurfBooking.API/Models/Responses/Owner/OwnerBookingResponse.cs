namespace TurfBooking.API.Models.Responses.Owner;

public class OwnerBookingResponse
{
    public int BookingId { get; set; }

    public int TurfId { get; set; }

    public string TurfName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerPhone { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Amount { get; set; }

    public string BookingStatus { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}