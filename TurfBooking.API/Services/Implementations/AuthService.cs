using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Helpers;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests;
using TurfBooking.API.Models.Responses;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Check Email
        var existingEmail = await _userManager.FindByEmailAsync(request.Email);

        if (existingEmail != null)
        {
            return ApiResponse<AuthResponse>.FailureResponse("Email already exists.");
        }

        // Check Phone Number
        var existingPhone = await _userManager.Users
    .FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber);

        if (existingPhone != null)
        {
            return ApiResponse<AuthResponse>.FailureResponse("Phone number already exists.");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                "Registration failed.",
                result.Errors.Select(x => x.Description).ToList());
        }

        await _userManager.AddToRoleAsync(user, "User");



        var response = new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber!,
            Role = "User"
        };

        return ApiResponse<AuthResponse>.SuccessResponse(
            response,
            "Registration successful. Please login.");

    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        ApplicationUser? user;

        // Login using Email or Phone
        if (request.EmailOrPhone.Contains("@"))
        {
            user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
        }
        else
        {
            user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.PhoneNumber == request.EmailOrPhone);
        }

        if (user == null)
        {
            throw new UnauthorizedException("Invalid email/phone or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            throw new UnauthorizedException("Invalid email/phone or password.");
        }

        var token = await _jwtTokenGenerator.GenerateTokenAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var response = new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber!,
            Role = roles.FirstOrDefault() ?? "User",
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return ApiResponse<AuthResponse>.SuccessResponse(
            response,
            "Login successful.");
    }
}