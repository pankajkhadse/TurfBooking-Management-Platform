using TurfBooking.API.Enums;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services;

public class BookingLifecycleService : IBookingLifecycleService
{
    public void ExpireBooking(Booking booking)
    {
        booking.Status = BookingStatus.Expired;

        foreach (var slot in booking.BookingSlots)
        {
            slot.IsActive = false;
        }
    }
}