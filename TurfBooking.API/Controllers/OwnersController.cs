using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurfBooking.API.Models.Requests.Owner;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Controllers;

[Route("api")]
[ApiController]
[Authorize]
public class OwnersController : ControllerBase
{
    private readonly IOwnerService _ownerService;

    public OwnersController(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    [HttpPost("owner-applications")]
    public async Task<IActionResult> Apply(ApplyOwnerRequest request)
    {
        var result = await _ownerService.ApplyAsync(request);

        return Ok(result);
    }


    [HttpGet("owner-applications/me")]
    public async Task<IActionResult> GetMyApplication()
    {
        var result = await _ownerService.GetMyApplicationAsync();

        return Ok(result);
    }

    [HttpGet("owner/dashboard")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _ownerService.GetDashboardAsync();

        return Ok(result);
    }

    [HttpGet("owner/bookings")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetOwnerBookings(
    [FromQuery] OwnerBookingRequest request)
    {
        var result = await _ownerService.GetOwnerBookingsAsync(request);

        return Ok(result);
    }

}