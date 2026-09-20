using System.ComponentModel.DataAnnotations;

namespace TurfBooking.API.Models.Requests.Admin;

public class RejectOwnerRequest
{
    [Required]
    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
}