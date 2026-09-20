using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests.Booking;
using TurfBooking.API.Models.Responses.Booking;
using TurfBooking.API.Models.Responses.Common;
using TurfBooking.API.Services.Interfaces;
using TurfBooking.API.Settings;

namespace TurfBooking.API.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly BookingSettings _bookingSettings;

    private readonly IBookingLifecycleService _bookingLifecycleService;

    private readonly IRefundService _refundService;
    private readonly ILogger<BookingService> _logger;
    public BookingService(ApplicationDbContext context, ICurrentUserService currentUser, IOptions<BookingSettings> bookingSettings, IBookingLifecycleService bookingLifecycleService, IRefundService refundService, ILogger<BookingService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _bookingSettings = bookingSettings.Value;
        _bookingLifecycleService = bookingLifecycleService;
        _refundService = refundService;
        _logger = logger;
    }



    public async Task<ApiResponse<BookingResponse>> CreateAsync(
    CreateBookingRequest request)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException(
                "User is not authenticated.");

        // -------------------------------------------------
        // 1. Get active turf
        // -------------------------------------------------

        var turf = await _context.Turfs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.TurfId &&
                x.Status == TurfStatus.Active);

        if (turf == null)
            throw new NotFoundException("Turf not found.");

        // -------------------------------------------------
        // 2. Validate slot duration configuration
        // -------------------------------------------------

        if (turf.SlotDurationMinutes <= 0 ||
            turf.SlotDurationMinutes > 1440)
        {
            throw new BadRequestException(
                "Invalid turf slot duration configuration.");
        }

        // -------------------------------------------------
        // 3. Normalize booking date
        // -------------------------------------------------

        var bookingDate = request.BookingDate.Date;
        var today = DateTime.UtcNow.Date;

        if (bookingDate < today)
        {
            throw new BadRequestException(
                "Booking date cannot be in the past.");
        }

        // -------------------------------------------------
        // 4. Validate booking advance limit
        // -------------------------------------------------

        var maximumDate =
            today.AddDays(turf.BookingAdvanceDays);

        if (bookingDate > maximumDate)
        {
            throw new BadRequestException(
                $"This turf can only be booked " +
                $"{turf.BookingAdvanceDays} days in advance.");
        }

        // -------------------------------------------------
        // 5. Get weekly schedule
        // -------------------------------------------------

        var schedule = await _context.TurfSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TurfId == turf.Id &&
                x.DayOfWeek == bookingDate.DayOfWeek);

        if (schedule == null || !schedule.IsAvailable)
        {
            throw new BadRequestException(
                "Turf is closed on the selected day.");
        }

        // -------------------------------------------------
        // 6. Validate schedule configuration
        // -------------------------------------------------

        if (schedule.StartTime >= schedule.EndTime)
        {
            throw new BadRequestException(
                "Invalid turf schedule configuration.");
        }

        // -------------------------------------------------
        // 7. Validate requested time range
        // -------------------------------------------------

        if (request.StartTime >= request.EndTime)
        {
            throw new BadRequestException(
                "Invalid booking time.");
        }

        if (request.StartTime < schedule.StartTime ||
            request.EndTime > schedule.EndTime)
        {
            throw new BadRequestException(
                "Selected time is outside the turf schedule.");
        }

        // -------------------------------------------------
        // 8. Calculate requested duration
        // -------------------------------------------------

        var requestedDuration =
            request.EndTime.ToTimeSpan() -
            request.StartTime.ToTimeSpan();

        var slotDuration =
            TimeSpan.FromMinutes(turf.SlotDurationMinutes);

        // -------------------------------------------------
        // 9. Requested duration must contain
        //    one or more complete slots
        // -------------------------------------------------

        if (requestedDuration < slotDuration)
        {
            throw new BadRequestException(
                $"Minimum booking duration is " +
                $"{turf.SlotDurationMinutes} minutes.");
        }

        var totalMinutes = requestedDuration.TotalMinutes;

        if (totalMinutes % turf.SlotDurationMinutes != 0)
        {
            throw new BadRequestException(
                $"Booking duration must be a multiple of " +
                $"{turf.SlotDurationMinutes} minutes.");
        }

        // -------------------------------------------------
        // 10. Start time must align with turf slots
        // -------------------------------------------------
        //
        // Example:
        //
        // Schedule = 09:00
        // Duration = 60 minutes
        //
        // Valid:
        // 09:00
        // 10:00
        // 11:00
        //
        // Invalid:
        // 09:30
        // 10:30
        //
        // -------------------------------------------------

        var scheduleStart =
            schedule.StartTime.ToTimeSpan();

        var requestedStart =
            request.StartTime.ToTimeSpan();

        var minutesFromScheduleStart =
            (requestedStart - scheduleStart).TotalMinutes;

        if (minutesFromScheduleStart < 0 ||
            minutesFromScheduleStart %
            turf.SlotDurationMinutes != 0)
        {
            throw new BadRequestException(
                "Booking start time does not align with the turf slots.");
        }

        // -------------------------------------------------
        // 11. Calculate number of slots
        // -------------------------------------------------

        var slotCount =
            (int)(totalMinutes / turf.SlotDurationMinutes);

        if (slotCount <= 0)
        {
            throw new BadRequestException(
                "Invalid booking slot count.");
        }

        // Safety protection
        if (slotCount > 96)
        {
            throw new BadRequestException(
                "Too many slots requested in a single booking.");
        }

        // -------------------------------------------------
        // 12. Generate individual slots
        // -------------------------------------------------

        var bookingSlots = new List<BookingSlot>();

        var currentStart =
            request.StartTime.ToTimeSpan();

        for (var i = 0; i < slotCount; i++)
        {
            var currentEnd =
                currentStart + slotDuration;

            var slotStart =
                TimeOnly.FromTimeSpan(currentStart);

            var slotEnd =
                TimeOnly.FromTimeSpan(currentEnd);

            bookingSlots.Add(new BookingSlot
            {
                TurfId = turf.Id,
                BookingDate = bookingDate,
                StartTime = slotStart,
                EndTime = slotEnd,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            currentStart = currentEnd;
        }

        // -------------------------------------------------
        // 13. Expire old pending bookings
        // -------------------------------------------------
        //
        // Important:
        //
        // Suppose User A created:
        //
        // 09:00 - 11:00
        //
        // but did not pay.
        //
        // After expiry, those slots must be released.
        //
        // -------------------------------------------------

        var now = DateTime.UtcNow;

        var expiredBookings = await _context.Bookings
            .Include(x => x.BookingSlots)
            .Where(x =>
                x.TurfId == turf.Id &&
                x.BookingDate == bookingDate &&
                x.Status == BookingStatus.Pending &&
                x.ExpiresAt.HasValue &&
                x.ExpiresAt.Value <= now &&
                x.BookingSlots.Any(s => s.IsActive))
            .ToListAsync();

        foreach (var expiredBooking in expiredBookings)
        {
            _bookingLifecycleService.ExpireBooking(expiredBooking);
        }

        // -------------------------------------------------
        // 14. Check whether requested slots are already
        //     reserved
        // -------------------------------------------------

        var requestedStartTimes =
            bookingSlots
                .Select(x => x.StartTime)
                .ToList();

        var conflictingSlots = await _context.BookingSlots
            .AsNoTracking()
            .Where(x =>
                x.TurfId == turf.Id &&
                x.BookingDate == bookingDate &&
                x.IsActive &&
                requestedStartTimes.Contains(x.StartTime))
            .Select(x => x.StartTime)
            .ToListAsync();

        if (conflictingSlots.Count > 0)
        {
            throw new BadRequestException(
                "One or more selected slots are no longer available.");
        }

        // -------------------------------------------------
        // 15. Calculate total booking amount
        // -------------------------------------------------

        var totalAmount =
            turf.PricePerSlot * slotCount;

        if (totalAmount <= 0)
        {
            throw new BadRequestException(
                "Invalid booking amount.");
        }

        // -------------------------------------------------
        // 16. Create parent Booking
        // -------------------------------------------------

        var booking = new Booking
        {
            TurfId = turf.Id,
            UserId = userId,
            BookingDate = bookingDate,

            StartTime = request.StartTime,
            EndTime = request.EndTime,

            Amount = totalAmount,

            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,

            ExpiresAt =
                now.AddMinutes(
                    _bookingSettings.PaymentExpiryMinutes),

            CreatedAt = now
        };

        // -------------------------------------------------
        // 17. Connect child slots to parent booking
        // -------------------------------------------------

        foreach (var slot in bookingSlots)
        {
            booking.BookingSlots.Add(slot);
        }

        // -------------------------------------------------
        // 18. Add booking
        // -------------------------------------------------

        _context.Bookings.Add(booking);

        // -------------------------------------------------
        // 19. Save booking + all slots together
        // -------------------------------------------------

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // SQL Server unique constraint violation:
            //
            // 2601 = duplicate key
            // 2627 = unique constraint violation
            //
            // This protects against two users booking
            // the same slot at exactly the same time.

            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlException &&
                (sqlException.Number == 2601 ||
                 sqlException.Number == 2627))
            {
                throw new BadRequestException(
                    "One or more selected slots were booked by another user. Please select the slots again.");
            }

            throw;
        }

        // -------------------------------------------------
        // 20. Build response
        // -------------------------------------------------

        var response = new BookingResponse
        {
            Id = booking.Id,

            TurfId = turf.Id,

            TurfName = turf.Name,

            BookingDate = booking.BookingDate,

            StartTime = booking.StartTime,

            EndTime = booking.EndTime,

            Amount = booking.Amount,

            Status = booking.Status,

            PaymentStatus = booking.PaymentStatus,

            CreatedAt = booking.CreatedAt,

            ExpiresAt = booking.ExpiresAt
        };

        return ApiResponse<BookingResponse>.SuccessResponse(
            response,
            "Booking created and slots temporarily reserved.");
    }

    public async Task<ApiResponse<PagedResponse<BookingResponse>>> GetMyBookingsAsync(
    MyBookingsRequest request)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException("User is not authenticated.");

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
        // Base query
        // -----------------------------------------

        var query = _context.Bookings
            .AsNoTracking()
            .Where(x => x.UserId == userId);

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
        // From date filter
        // -----------------------------------------

        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value.Date;

            query = query.Where(x =>
                x.BookingDate >= fromDate);
        }

        // -----------------------------------------
        // To date filter
        // -----------------------------------------

        if (request.ToDate.HasValue)
        {
            var toDate = request.ToDate.Value
                .Date
                .AddDays(1);

            query = query.Where(x =>
                x.BookingDate < toDate);
        }

        // -----------------------------------------
        // Total records
        // -----------------------------------------

        var totalCount = await query.CountAsync();

        // -----------------------------------------
        // Get current page
        // -----------------------------------------

        var bookings = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new BookingResponse
            {
                Id = x.Id,
                TurfId = x.TurfId,
                TurfName = x.Turf.Name,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Amount = x.Amount,
                Status = x.Status,
                PaymentStatus = x.PaymentStatus,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt,
                ConfirmedAt = x.ConfirmedAt,
                CancelledAt = x.CancelledAt
            })
            .ToListAsync();

        // -----------------------------------------
        // Calculate total pages
        // -----------------------------------------

        var totalPages =
            (int)Math.Ceiling(
                totalCount / (double)request.PageSize);

        // -----------------------------------------
        // Build paginated response
        // -----------------------------------------

        var response = new PagedResponse<BookingResponse>
        {
            Items = bookings,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return ApiResponse<PagedResponse<BookingResponse>>
            .SuccessResponse(
                response,
                "Bookings retrieved successfully.");
    }

    public async Task<ApiResponse<BookingResponse>> GetBookingByIdAsync(
    int bookingId)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException("User is not authenticated.");

        var booking = await _context.Bookings
            .AsNoTracking()
            .Where(x =>
                x.Id == bookingId &&
                x.UserId == userId)
            .Select(x => new BookingResponse
            {
                Id = x.Id,
                TurfId = x.TurfId,
                TurfName = x.Turf.Name,
                BookingDate = x.BookingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Amount = x.Amount,
                Status = x.Status,
                PaymentStatus = x.PaymentStatus,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt,
                ConfirmedAt = x.ConfirmedAt,
                CancelledAt = x.CancelledAt
            })
            .FirstOrDefaultAsync();

        if (booking == null)
            throw new NotFoundException("Booking not found.");

        return ApiResponse<BookingResponse>.SuccessResponse(
            booking,
            "Booking retrieved successfully.");
    }

    public async Task<ApiResponse<BookingResponse>> CancelBookingAsync(
        int bookingId)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException("User is not authenticated.");

        // Load the booking, its reserved slots, and its payment.
        var booking = await _context.Bookings
            .Include(x => x.Turf)
            .Include(x => x.BookingSlots)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x =>
                x.Id == bookingId &&
                x.UserId == userId);

        if (booking == null)
            throw new NotFoundException("Booking not found.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new BadRequestException("Booking is already cancelled.");

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
        {
            throw new BadRequestException("This booking cannot be cancelled.");
        }

        var now = DateTime.UtcNow;
        var scheduledStart = booking.BookingDate.Date
            .Add(booking.StartTime.ToTimeSpan());
        var timeUntilStart = scheduledStart - now;

        // Cancellation is blocked inside the final two hours.
        if (timeUntilStart < TimeSpan.FromHours(2))
        {
            throw new BadRequestException(
                "Bookings cannot be cancelled less than 2 hours before the scheduled start time.");
        }

        // Recommended default policy: >24h = 95%, 12-24h = 50%,
        // 2-12h = 25%. The policy should be shown before checkout.

        decimal refundPercentage = timeUntilStart > TimeSpan.FromHours(24)
            ? 0.95m
            : timeUntilStart >= TimeSpan.FromHours(12)
                ? 0.50m
                : 0.25m;


        var payment = booking.Payment;
        decimal refundAmount = 0m;

        if (payment?.Status == PaymentStatus.Paid && refundPercentage > 0m)
        {
            refundAmount = decimal.Round(
                payment.Amount * refundPercentage,
                2,
                MidpointRounding.AwayFromZero);
        }

        // Persist cancellation and release slots first. Refund processing is
        // separate because Razorpay is an external service.
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.ExpiresAt = null;

        foreach (var slot in booking.BookingSlots)
            slot.IsActive = false;

        await _context.SaveChangesAsync();

        var refundNeedsAttention = false;

        if (payment?.Status == PaymentStatus.Paid && refundAmount > 0m)
        {
            try
            {
                await _refundService.ProcessCancellationRefundAsync(
                    payment.Id,
                    refundAmount,
                    booking.Id);
            }
            catch (Exception ex)
            {
                // Cancellation remains committed; log the refund failure so
                // it can be reviewed/retried rather than losing the request.
                refundNeedsAttention = true;
                _logger.LogError(
                    ex,
                    "Cancellation refund failed or needs retry for booking {BookingId}, payment {PaymentId}, amount {RefundAmount}.",
                    booking.Id,
                    payment.Id,
                    refundAmount);
            }
        }

        var response = new BookingResponse
        {
            Id = booking.Id,
            TurfId = booking.TurfId,
            TurfName = booking.Turf.Name,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Amount = booking.Amount,
            Status = booking.Status,
            PaymentStatus = booking.PaymentStatus,
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt
        };

        var message = refundNeedsAttention
            ? "Booking cancelled and slots released. The refund could not be confirmed; it requires retry or review."
            : refundAmount > 0m
                ? "Booking cancelled and slots released. Refund processing has been initiated; final status will update from Razorpay."
                : payment?.Status == PaymentStatus.Paid
                    ? "Booking cancelled and slots released. No refund is due under the cancellation policy."
                    : "Booking cancelled successfully and slots released. No payment refund is required.";

        return ApiResponse<BookingResponse>.SuccessResponse(response, message);
    }
}
