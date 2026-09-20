using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Helpers;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests.Admin;
using TurfBooking.API.Models.Responses.Admin;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApiResponse<List<OwnerApplicationResponse>>> GetPendingOwnerApplicationsAsync()
    {
        var applications = await _context.OwnerProfiles
            .Include(x => x.ApplicationUser)
            .Where(x => x.Status == OwnerStatus.Pending)
            .AsNoTracking()
            .Select(x => new OwnerApplicationResponse
            {
                Id = x.Id,
                UserId = x.ApplicationUserId,
                FullName = x.ApplicationUser.FullName,
                Email = x.ApplicationUser.Email!,
                PhoneNumber = x.PhoneNumber,
                BusinessName = x.BusinessName,
                ContactPersonName = x.ContactPersonName,
                City = x.City,
                Status = x.Status.ToString(),
                AppliedAt = x.AppliedAt
            })
            .ToListAsync();

        return ApiResponse<List<OwnerApplicationResponse>>
            .SuccessResponse(applications,"Get Successfully");
    }

    public async Task<ApiResponse<string>> ApproveOwnerApplicationAsync(int ownerProfileId)
    {
        var owner = await _context.OwnerProfiles
            .FirstOrDefaultAsync(x => x.Id == ownerProfileId);

        if (owner == null)
            throw new NotFoundException("Owner application not found.");

        if (owner.Status != OwnerStatus.Pending)
            throw new BadRequestException("Application has already been processed.");

        var user = await _userManager.FindByIdAsync(owner.ApplicationUserId);

        if (user == null)
            throw new NotFoundException("User not found.");

        owner.Status = OwnerStatus.Approved;
        owner.ApprovedAt = DateTime.UtcNow;

        var roleResult = await _userManager.AddToRoleAsync(user, "Owner");

        if (!roleResult.Succeeded)
            throw new BadRequestException("Failed to assign Owner role.");

        await _context.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse(
            "Owner approved successfully.");
    }

    public async Task<ApiResponse<string>> RejectOwnerApplicationAsync(
        int ownerProfileId,
        RejectOwnerRequest request)
    {
        var owner = await _context.OwnerProfiles
            .FirstOrDefaultAsync(x => x.Id == ownerProfileId);

        if (owner == null)
            throw new NotFoundException("Owner application not found.");

        if (owner.Status != OwnerStatus.Pending)
            throw new BadRequestException("Application has already been processed.");

        owner.Status = OwnerStatus.Rejected;
        owner.Remarks = request.Remarks;

        await _context.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse(
            "Owner application rejected.");
    }
}