namespace TurfBooking.API.Models.Requests.Booking;

public class MyBookingsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int? TurfId { get; set; }

    public string? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}