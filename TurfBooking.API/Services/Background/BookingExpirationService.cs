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
                await ExpirePendingBookingsAsync(stoppingToken);
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
                    "Error while expiring pending bookings.");
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