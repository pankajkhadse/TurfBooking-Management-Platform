using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Services.Interfaces;

public interface IBookingLifecycleService
{
    void ExpireBooking(Booking booking);
}