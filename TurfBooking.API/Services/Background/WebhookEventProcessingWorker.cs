
using Microsoft.EntityFrameworkCore;
using TurfBooking.API.Data;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Services.Background;

public class WebhookEventProcessingWorker : BackgroundService
{
    private const int MaxAttempts = 10;
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookEventProcessingWorker> _logger;

    public WebhookEventProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookEventProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Webhook event processing worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
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
                    "Unexpected error in webhook processing worker.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Webhook event processing worker stopped.");
    }

    private async Task ProcessPendingEventsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Process only event types that currently have handlers.
        var pendingEvents = await context.WebhookEvents
            .AsNoTracking()
            .Where(x =>
                !x.IsProcessed &&
                x.ProcessingAttempts < MaxAttempts &&
                (x.NextAttemptAt == null ||
                 x.NextAttemptAt <= now) &&
                (x.EventType == "payment.captured" ||
                 x.EventType == "payment.authorized" ||
                 x.EventType == "payment.failed"))
            .OrderBy(x => x.ReceivedAt)
            .Select(x => new
            {
                x.Id,
                x.ProcessingAttempts
            })
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in pendingEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = item.ProcessingAttempts + 1;
            var attemptedAt = DateTime.UtcNow;

            // Exponential retry delay: 2, 4, 8, 16... minutes,
            // capped at 60 minutes.
            var delayMinutes = Math.Min(
                60,
                Math.Pow(2, attempt));

            var nextAttemptAt = attempt < MaxAttempts
                ? attemptedAt.AddMinutes(delayMinutes)
                : (DateTime?)null;

            // Atomically claim this attempt. This reduces the risk
            // of two application instances processing the same event.
            var claimed = await context.WebhookEvents
                .Where(x =>
                    x.Id == item.Id &&
                    !x.IsProcessed &&
                    x.ProcessingAttempts == item.ProcessingAttempts &&
                    (x.NextAttemptAt == null ||
                     x.NextAttemptAt <= attemptedAt))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.ProcessingAttempts,
                            attempt)
                        .SetProperty(
                            x => x.LastAttemptAt,
                            (DateTime?)attemptedAt)
                        .SetProperty(
                            x => x.NextAttemptAt,
                            nextAttemptAt),
                    cancellationToken);

            if (claimed == 0)
                continue;

            try
            {
                using var processingScope =
                    _scopeFactory.CreateScope();

                var processor = processingScope.ServiceProvider
                    .GetRequiredService<
                        IWebhookEventProcessingService>();

                await processor.ProcessAsync(
                    item.Id,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Webhook event {WebhookEventId} failed on attempt {Attempt}/{MaxAttempts}.",
                    item.Id,
                    attempt,
                    MaxAttempts);

                if (attempt >= MaxAttempts)
                {
                    _logger.LogError(
                        "Webhook event {WebhookEventId} reached the retry limit. Manual investigation is required.",
                        item.Id);
                }
            }
        }
    }
}
