namespace TurfBooking.API.Models.Domain;

public class OwnerProfile
{
    public int Id { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public string BusinessName { get; set; } = string.Empty;

    public string ContactPersonName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public OwnerStatus Status { get; set; } = OwnerStatus.Pending;

    public string? Remarks { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }

    public ICollection<Turf> Turfs { get; set; } = new List<Turf>();
}