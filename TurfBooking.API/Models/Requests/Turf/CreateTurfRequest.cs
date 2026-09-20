using System.ComponentModel.DataAnnotations;

namespace TurfBooking.API.Models.Requests.Turf;

public class CreateTurfRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Locality { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public decimal Latitude { get; set; }

    [Required]
    public decimal Longitude { get; set; }

    [Range(1, 10000)]
    public decimal PricePerSlot { get; set; }

    [Range(30, 180)]
    public int SlotDurationMinutes { get; set; }
}