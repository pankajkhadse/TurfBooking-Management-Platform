namespace TurfBooking.API.Models.Responses.Owner;

public class OwnerProfileResponse
{
    public int Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string ContactPersonName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }
}