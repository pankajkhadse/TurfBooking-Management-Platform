
using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Services.Interfaces;

public interface IWebhookEventProcessingService
{
    Task ProcessAsync(
        int webhookEventId,
        CancellationToken cancellationToken = default);
}
