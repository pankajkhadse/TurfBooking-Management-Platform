using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Domain;

public class Booking
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public Turf Turf { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Amount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Payment? Payment { get; set; }

    public ICollection<BookingSlot> BookingSlots { get; set; }
    = new List<BookingSlot>();
}