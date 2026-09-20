using System.ComponentModel.DataAnnotations;
using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Requests.Turf;

public class UpdateTurfSportsRequest
{
    [Required]
    public List<SportType> Sports { get; set; } = new();
}