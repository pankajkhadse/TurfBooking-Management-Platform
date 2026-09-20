using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests.Booking;
using TurfBooking.API.Models.Responses.Booking;
using TurfBooking.API.Models.Responses.Common;

namespace TurfBooking.API.Services.Interfaces;

public interface IBookingService
{
    Task<ApiResponse<BookingResponse>> CreateAsync(
        CreateBookingRequest request);

    Task<ApiResponse<PagedResponse<BookingResponse>>> GetMyBookingsAsync(
        MyBookingsRequest request);

    Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(
        int bookingId);

    Task<ApiResponse<BookingResponse>> CancelBookingAsync(
        int bookingId);
}