using System.ComponentModel.DataAnnotations;

namespace TurfBooking.API.Models.Requests.Owner;

public class ApplyOwnerRequest
{
    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;
}