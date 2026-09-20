using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurfBooking.API.Models.Requests.Turf;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/turfs")]
[Authorize(Roles = "Owner")]
public class TurfsController : ControllerBase
{
    private readonly ITurfService _turfService;

    public TurfsController(ITurfService turfService)
    {
        _turfService = turfService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTurfRequest request)
    {
        var result = await _turfService.CreateAsync(request);

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyTurfs()
    {
        return Ok(await _turfService.GetMyTurfsAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _turfService.GetByIdAsync(id));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        CreateTurfRequest request)
    {
        return Ok(await _turfService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        return Ok(await _turfService.DeactivateAsync(id));
    }



    [HttpPost("{id:int}/images")]
    public async Task<IActionResult> UploadImages(
    int id,
    List<IFormFile> files)
    {
        return Ok(await _turfService.UploadImagesAsync(id, files));
    }

    [HttpPut("{id:int}/sports")]
    public async Task<IActionResult> UpdateSports(
    int id,
    UpdateTurfSportsRequest request)
    {
        return Ok(await _turfService.UpdateSportsAsync(id, request));
    }


    [HttpPut("{id:int}/amenities")]
    public async Task<IActionResult> UpdateAmenities(
    int id,
    [FromBody] UpdateTurfAmenitiesRequest request)
    {
        return Ok(await _turfService.UpdateAmenitiesAsync(id, request));
    }


    [HttpPut("{id:int}/schedule")]
    public async Task<IActionResult> UpdateSchedule(int id,[FromBody] UpdateTurfScheduleRequest request)
    {
        return Ok(await _turfService.UpdateScheduleAsync(id, request));
    }


    [HttpGet("{id:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        return Ok(await _turfService.GetScheduleAsync(id));
    }



    [HttpGet("{turfId:int}/available-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableSlots(
     int turfId,
     [FromQuery] DateTime date)
    {
        var result = await _turfService.GetAvailableSlotsAsync(
            turfId,
            date);

        return Ok(result);
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearbyTurfs(
    [FromQuery] NearbyTurfRequest request)
    {
        var result = await _turfService.GetNearbyTurfsAsync(request);

        return Ok(result);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchTurfs(
    [FromQuery] TurfSearchRequest request)
    {
        var result =
            await _turfService.SearchTurfsAsync(request);

        return Ok(result);
    }

}