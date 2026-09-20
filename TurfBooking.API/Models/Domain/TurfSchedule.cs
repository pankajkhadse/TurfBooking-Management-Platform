namespace TurfBooking.API.Models.Domain;

public class TurfSchedule
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public Turf Turf { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}