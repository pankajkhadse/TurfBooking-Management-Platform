namespace TurfBooking.API.Models.Responses.Owner;

public class OwnerDashboardResponse
{
    public int TotalTurfs { get; set; }

    public int ActiveTurfs { get; set; }

    public int TotalBookings { get; set; }

    public int TodayBookings { get; set; }

    public int UpcomingBookings { get; set; }

    public int CompletedBookings { get; set; }

    public int CancelledBookings { get; set; }

    public decimal TotalRevenue { get; set; }

    public List<OwnerRecentBookingResponse> RecentBookings { get; set; }
        = new();
}