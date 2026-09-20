namespace TurfBooking.API.Models.Responses.Turf;

public class TurfScheduleResponse
{
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsAvailable { get; set; }
}