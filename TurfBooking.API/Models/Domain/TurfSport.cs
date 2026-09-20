using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Domain;

public class TurfSport
{
    public int Id { get; set; }

    public int TurfId { get; set; }

    public Turf Turf { get; set; } = null!;

    public SportType SportType { get; set; }
}