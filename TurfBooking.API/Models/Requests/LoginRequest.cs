using System.ComponentModel.DataAnnotations;

namespace TurfBooking.API.Models.Requests;

public class LoginRequest
{
    [Required]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}