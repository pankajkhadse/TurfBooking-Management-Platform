namespace TurfBooking.API.Services.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}