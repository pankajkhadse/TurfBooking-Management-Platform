namespace TurfBooking.API.Models.Domain;

public class Sport
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation Properties
    public ICollection<TurfSport> TurfSports { get; set; } = new List<TurfSport>();
}