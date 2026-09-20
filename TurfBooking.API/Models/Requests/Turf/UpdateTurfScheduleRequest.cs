using System.ComponentModel.DataAnnotations;

namespace TurfBooking.API.Models.Requests.Turf;

public class UpdateTurfScheduleRequest
{
    [Required]
    public List<TurfScheduleRequest> Schedules { get; set; } = new();
}

public class TurfScheduleRequest
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsAvailable { get; set; }
}