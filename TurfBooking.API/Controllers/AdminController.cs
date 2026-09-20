using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurfBooking.API.Models.Requests.Admin;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("owner-applications")]
    public async Task<IActionResult> GetPendingApplications()
    {
        var result = await _adminService.GetPendingOwnerApplicationsAsync();

        return Ok(result);
    }

    [HttpPut("owner-applications/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _adminService.ApproveOwnerApplicationAsync(id);

        return Ok(result);
    }

    [HttpPut("owner-applications/{id:int}/reject")]
    public async Task<IActionResult> Reject(
        int id,
        RejectOwnerRequest request)
    {
        var result = await _adminService.RejectOwnerApplicationAsync(id, request);

        return Ok(result);
    }
}