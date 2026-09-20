namespace TurfBooking.API.Models.Responses.Admin;

public class OwnerApplicationResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;

    public string ContactPersonName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }
}