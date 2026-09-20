using TurfBooking.API.Helpers;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests.Admin;
using TurfBooking.API.Models.Responses.Admin;

namespace TurfBooking.API.Services.Interfaces;

public interface IAdminService
{
    Task<ApiResponse<List<OwnerApplicationResponse>>> GetPendingOwnerApplicationsAsync();

    Task<ApiResponse<string>> ApproveOwnerApplicationAsync(int ownerProfileId);

    Task<ApiResponse<string>> RejectOwnerApplicationAsync(
        int ownerProfileId,
        RejectOwnerRequest request);
}