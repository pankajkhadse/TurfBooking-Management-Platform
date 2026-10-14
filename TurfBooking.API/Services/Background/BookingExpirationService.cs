using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Background;

public class BookingExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationService> _logger;

    public BookingExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Expire pending bookings
                await ExpirePendingBookingsAsync(stoppingToken);

                // 2. Mark finished bookings as Completed
                await CompleteFinishedBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing booking lifecycle.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }



    private async Task CompleteFinishedBookingsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var nowUtc = DateTime.UtcNow;

        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "India Standard Time"
                : "Asia/Kolkata");

        // BookingDate and EndTime represent Indian local time.
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(
            nowUtc,
            indiaTimeZone);

        var todayIst = nowIst.Date;

        // Load only paid, confirmed bookings that may have finished.
        var candidateBookings = await context.Bookings
            .Where(x =>
                x.Status == BookingStatus.Confirmed &&
                x.PaymentStatus == PaymentStatus.Paid &&
                x.BookingDate <= todayIst)
            .ToListAsync(cancellationToken);

        var completedCount = 0;

        foreach (var booking in candidateBookings)
        {
            // Reconstruct the scheduled end time in Indian local time.
            var localEndTime = DateTime.SpecifyKind(
                booking.BookingDate.Date.Add(
                    booking.EndTime.ToTimeSpan()),
                DateTimeKind.Unspecified);

            // Convert the scheduled end time to UTC before comparison.
            var scheduledEndUtc = TimeZoneInfo.ConvertTimeToUtc(
                localEndTime,
                indiaTimeZone);

            if (scheduledEndUtc <= nowUtc)
            {
                booking.Status = BookingStatus.Completed;
                completedCount++;
            }
        }

        if (completedCount == 0)
            return;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Count} paid bookings marked as completed.",
            completedCount);
    }


    private async Task ExpirePendingBookingsAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var lifecycleService =
            scope.ServiceProvider
                .GetRequiredService<IBookingLifecycleService>();

        var now = DateTime.UtcNow;

        var expiredBookings = await context.Bookings
            .Include(x => x.BookingSlots)
            .Where(x =>
                x.Status == BookingStatus.Pending &&
                x.ExpiresAt != null &&
                x.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0)
            return;

        foreach (var booking in expiredBookings)
        {
            lifecycleService.ExpireBooking(booking);
        }

        await context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "{Count} pending bookings expired and their slots were released.",
            expiredBookings.Count);
    }
}