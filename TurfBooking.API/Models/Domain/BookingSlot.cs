namespace TurfBooking.API.Models.Domain;

public class BookingSlot
{
    public int Id { get; set; }

    // Parent booking
    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    // Turf information
    public int TurfId { get; set; }

    public Turf Turf { get; set; } = null!;

    // Individual slot information
    public DateTime BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    // Used by the database unique index.
    // true = this slot is currently reserved
    // false = this slot has been released
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}