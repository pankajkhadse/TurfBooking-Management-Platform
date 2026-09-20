using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurfBooking.API.Models.Requests.Booking;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Controllers;

[Route("api/bookings")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.CreateAsync(request);

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings(
     [FromQuery] MyBookingsRequest request)
    {
        var result = await _bookingService.GetMyBookingsAsync(request);

        return Ok(result);
    }


    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> GetBookingById(int bookingId)
    {
        var result = await _bookingService.GetBookingByIdAsync(bookingId);

        return Ok(result);
    }

    [HttpPost("{bookingId:int}/cancel")]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        var result = await _bookingService.CancelBookingAsync(bookingId);

        return Ok(result);
    }
}