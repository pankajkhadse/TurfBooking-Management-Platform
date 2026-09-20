namespace TurfBooking.API.Models.Responses;

public class AuthResponse
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Token { get; set; }

    public DateTime? ExpiresAt { get; set; }
}