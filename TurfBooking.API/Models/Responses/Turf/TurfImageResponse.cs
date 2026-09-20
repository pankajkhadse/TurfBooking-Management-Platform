namespace TurfBooking.API.Models.Responses.Turf;

public class TurfImageResponse
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }
}