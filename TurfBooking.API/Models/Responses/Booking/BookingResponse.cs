using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Responses.Booking;

public class BookingResponse
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public string TurfName { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Amount { get; set; }

    public BookingStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}

