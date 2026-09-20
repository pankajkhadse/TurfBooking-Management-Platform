using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests;
using TurfBooking.API.Models.Responses;

namespace TurfBooking.API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);

    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
}