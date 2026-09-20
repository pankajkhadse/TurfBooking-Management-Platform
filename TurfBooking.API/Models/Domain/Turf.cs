namespace TurfBooking.API.Models.Domain;

public class Turf
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Pincode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Locality { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal PricePerSlot { get; set; }

    public string? ContactNumber { get; set; }

    public string? GoogleMapLink { get; set; }

    public int SlotDurationMinutes { get; set; }

    public int BookingAdvanceDays { get; set; } = 7;

    public TurfStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Foreign Key
    public int OwnerProfileId { get; set; }

    // Navigation Properties
    public OwnerProfile OwnerProfile { get; set; } = null!;

    public ICollection<TurfImage> TurfImages { get; set; } = new List<TurfImage>();

    public ICollection<TurfSport> TurfSports { get; set; } = new List<TurfSport>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<TurfAmenity> TurfAmenities { get; set; } = new List<TurfAmenity>();

    //public ICollection<TurfSchedule> TurfSchedules { get; set; } = new List<TurfSchedule>();
    public ICollection<TurfSchedule> Schedules { get; set; }
    = new List<TurfSchedule>();

    //public ICollection<Booking> Bookings { get; set; }
    //= new List<Booking>();

    public ICollection<BookingSlot> BookingSlots { get; set; }
        = new List<BookingSlot>();

}