using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests.Owner;
using TurfBooking.API.Models.Responses.Common;
using TurfBooking.API.Models.Responses.Owner;

namespace TurfBooking.API.Services.Interfaces;

public interface IOwnerService
{
    Task<ApiResponse<OwnerProfileResponse>> ApplyAsync(
        ApplyOwnerRequest request);

    Task<ApiResponse<OwnerProfileResponse>> GetMyApplicationAsync();

    Task<ApiResponse<OwnerDashboardResponse>> GetDashboardAsync();

    Task<ApiResponse<PagedResponse<OwnerBookingResponse>>> GetOwnerBookingsAsync(
    OwnerBookingRequest request);
}