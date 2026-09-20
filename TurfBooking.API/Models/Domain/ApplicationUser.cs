using Microsoft.AspNetCore.Identity;

namespace TurfBooking.API.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public OwnerProfile? OwnerProfile { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}