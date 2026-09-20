using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests.Turf;
using TurfBooking.API.Models.Responses.Turf;

namespace TurfBooking.API.Services.Interfaces;

public interface ITurfService
{
    Task<ApiResponse<TurfResponse>> CreateAsync(CreateTurfRequest request);

    Task<ApiResponse<List<TurfResponse>>> GetMyTurfsAsync();

    Task<ApiResponse<TurfResponse>> GetByIdAsync(int id);

    Task<ApiResponse<TurfResponse>> UpdateAsync(int id, CreateTurfRequest request);

    Task<ApiResponse<string>> DeactivateAsync(int id);

    Task<ApiResponse<List<TurfImageResponse>>> UploadImagesAsync(
     int turfId,
     List<IFormFile> files);

    Task<ApiResponse<string>> UpdateSportsAsync(
    int turfId,
    UpdateTurfSportsRequest request);

    Task<ApiResponse<string>> UpdateAmenitiesAsync(
    int turfId,
    UpdateTurfAmenitiesRequest request);

    Task<ApiResponse<string>> UpdateScheduleAsync(
    int turfId,
    UpdateTurfScheduleRequest request);

    Task<ApiResponse<List<TurfScheduleResponse>>> GetScheduleAsync(
    int turfId);

    Task<ApiResponse<List<AvailableSlotResponse>>> GetAvailableSlotsAsync(
    int turfId,
    DateTime date);

    Task<ApiResponse<List<NearbyTurfResponse>>> GetNearbyTurfsAsync(
     NearbyTurfRequest request);

    Task<ApiResponse<List<NearbyTurfResponse>>> SearchTurfsAsync(
    TurfSearchRequest request);
}