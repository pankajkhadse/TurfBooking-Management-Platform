using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Helpers;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests.Owner;
using TurfBooking.API.Models.Responses.Common;
using TurfBooking.API.Models.Responses.Owner;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Implementations;

public class OwnerService : IOwnerService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public OwnerService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _context = context;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<OwnerProfileResponse>> ApplyAsync(ApplyOwnerRequest request)
    {
        var user = await _userManager.FindByIdAsync(_currentUser.UserId);

        if (user == null)
            throw new UnauthorizedException("User not found.");

        var existingOwner = await _context.OwnerProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

        if (existingOwner != null)
            throw new BadRequestException("You have already applied as an owner.");

        var owner = new OwnerProfile
        {
            ApplicationUserId = user.Id,
            BusinessName = request.BusinessName,
            ContactPersonName = request.ContactPersonName,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            City = request.City,
            Status = OwnerStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };

        _context.OwnerProfiles.Add(owner);

        await _context.SaveChangesAsync();

        var response = new OwnerProfileResponse
        {
            Id = owner.Id,
            BusinessName = owner.BusinessName,
            ContactPersonName = owner.ContactPersonName,
            PhoneNumber = owner.PhoneNumber,
            Address = owner.Address,
            City = owner.City,
            Status = owner.Status.ToString(),
            AppliedAt = owner.AppliedAt
        };

        return ApiResponse<OwnerProfileResponse>.SuccessResponse(
            response,
            "Owner application submitted successfully.");
    }

    public async Task<ApiResponse<OwnerProfileResponse>> GetMyApplicationAsync()
    {
        var owner = await _context.OwnerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == _currentUser.UserId);

        if (owner == null)
            throw new NotFoundException("Owner application not found.");

        var response = new OwnerProfileResponse
        {
            Id = owner.Id,
            BusinessName = owner.BusinessName,
            ContactPersonName = owner.ContactPersonName,
            PhoneNumber = owner.PhoneNumber,
            Address = owner.Address,
            City = owner.City,
            Status = owner.Status.ToString(),
            AppliedAt = owner.AppliedAt
        };

        return ApiResponse<OwnerProfileResponse>.SuccessResponse(response,"Application Get Successfully");
    }

    public async Task<ApiResponse<OwnerDashboardResponse>> GetDashboardAsync()
    {
        var owner = await _context.OwnerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ApplicationUserId == _currentUser.UserId &&
                x.Status == OwnerStatus.Approved);

        if (owner == null)
            throw new UnauthorizedException(
                "Owner profile not found or not approved.");

        // -----------------------------------------
        // Get owner's turfs
        // -----------------------------------------

        var turfQuery = _context.Turfs
            .AsNoTracking()
            .Where(x =>
                x.OwnerProfileId == owner.Id &&
                x.Status != TurfStatus.Deleted);

        var totalTurfs = await turfQuery.CountAsync();

        var activeTurfs = await turfQuery
            .CountAsync(x => x.Status == TurfStatus.Active);

        // -----------------------------------------
        // Get owner's bookings
        // -----------------------------------------

        var bookingQuery = _context.Bookings
            .AsNoTracking()
            .Where(x =>
                x.Turf.OwnerProfileId == owner.Id);

        var totalBookings = await bookingQuery.CountAsync();

        // -----------------------------------------
        // Today
        // -----------------------------------------

        var today = DateTime.UtcNow.Date;

        var tomorrow = today.AddDays(1);

        var todayBookings = await bookingQuery
            .CountAsync(x =>
                x.BookingDate >= today &&
                x.BookingDate < tomorrow);

        // -----------------------------------------
        // Upcoming bookings
        // -----------------------------------------

        var upcomingBookings = await bookingQuery
            .CountAsync(x =>
                x.BookingDate >= today &&
                x.Status == BookingStatus.Confirmed);

        // -----------------------------------------
        // Completed bookings
        // -----------------------------------------

        var completedBookings = await bookingQuery
            .CountAsync(x =>
                x.Status == BookingStatus.Completed);

        // -----------------------------------------
        // Cancelled bookings
        // -----------------------------------------

        var cancelledBookings = await bookingQuery
            .CountAsync(x =>
                x.Status == BookingStatus.Cancelled);

        // -----------------------------------------
        // Revenue
        // -----------------------------------------

        var totalRevenue = await bookingQuery
            .Where(x =>
                x.PaymentStatus == PaymentStatus.Paid &&
                x.Status != BookingStatus.Cancelled)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        // -----------------------------------------
        // Recent bookings
        // -----------------------------------------

        var recentBookings = await bookingQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new OwnerRecentBookingResponse
            {
                BookingId = x.Id,
                TurfId = x.TurfId,
                TurfName = x.Turf.Name,
                UserId = x.UserId,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Amount = x.Amount,
                BookingStatus = x.Status.ToString(),
                PaymentStatus = x.PaymentStatus.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        // -----------------------------------------
        // Response
        // -----------------------------------------

        var response = new OwnerDashboardResponse
        {
            TotalTurfs = totalTurfs,
            ActiveTurfs = activeTurfs,
            TotalBookings = totalBookings,
            TodayBookings = todayBookings,
            UpcomingBookings = upcomingBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            TotalRevenue = totalRevenue,
            RecentBookings = recentBookings
        };

        return ApiResponse<OwnerDashboardResponse>.SuccessResponse(
            response,
            "Owner dashboard retrieved successfully.");
    }

    public async Task<ApiResponse<PagedResponse<OwnerBookingResponse>>>
    GetOwnerBookingsAsync(OwnerBookingRequest request)
    {
        var owner = await _context.OwnerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ApplicationUserId == _currentUser.UserId &&
                x.Status == OwnerStatus.Approved);

        if (owner == null)
            throw new UnauthorizedException(
                "Owner profile not found or not approved.");

        // -----------------------------------------
        // Validate pagination
        // -----------------------------------------

        if (request.Page < 1)
            request.Page = 1;

        if (request.PageSize < 1)
            request.PageSize = 10;

        if (request.PageSize > 50)
            request.PageSize = 50;

        // -----------------------------------------
        // Owner's bookings
        // -----------------------------------------

        var query = _context.Bookings
            .AsNoTracking()
            .Where(x =>
                x.Turf.OwnerProfileId == owner.Id);

        // -----------------------------------------
        // Turf filter
        // -----------------------------------------

        if (request.TurfId.HasValue)
        {
            query = query.Where(x =>
                x.TurfId == request.TurfId.Value);
        }

        // -----------------------------------------
        // Status filter
        // -----------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<BookingStatus>(
                    request.Status,
                    true,
                    out var bookingStatus))
            {
                throw new BadRequestException(
                    "Invalid booking status.");
            }

            query = query.Where(x =>
                x.Status == bookingStatus);
        }

        // -----------------------------------------
        // From date
        // -----------------------------------------

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;

            query = query.Where(x =>
                x.BookingDate >= fromDate);
        }

        // -----------------------------------------
        // To date
        // -----------------------------------------

        if (request.ToDate.HasValue)
        {
            var toDate = request.ToDate.Value.Date.AddDays(1);

            query = query.Where(x =>
                x.BookingDate < toDate);
        }

        // -----------------------------------------
        // Total count
        // -----------------------------------------

        var totalCount = await query.CountAsync();

        // -----------------------------------------
        // Pagination
        // -----------------------------------------

        var bookings = await query
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OwnerBookingResponse
            {
                BookingId = x.Id,

                TurfId = x.TurfId,

                TurfName = x.Turf.Name,

                CustomerName = x.User.FullName,

                CustomerEmail = x.User.Email ?? string.Empty,

                CustomerPhone = x.User.PhoneNumber ?? string.Empty,

                BookingDate = x.BookingDate,

                StartTime = x.StartTime,

                EndTime = x.EndTime,

                Amount = x.Amount,

                BookingStatus = x.Status.ToString(),

                PaymentStatus = x.PaymentStatus.ToString(),

                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var totalPages =
            (int)Math.Ceiling(
                totalCount / (double)request.PageSize);

        var response = new PagedResponse<OwnerBookingResponse>
        {
            Items = bookings,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalCount = totalCount,

            TotalPages = totalPages
        };

        return ApiResponse<PagedResponse<OwnerBookingResponse>>
            .SuccessResponse(
                response,
                "Owner bookings retrieved successfully.");
    }

}