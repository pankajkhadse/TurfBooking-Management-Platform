
namespace TurfBooking.API.Models.Domain;

public class WebhookEvent
{
    public int Id { get; set; }

    // Unique event ID supplied by Razorpay.
    public string RazorpayEventId { get; set; } = string.Empty;

    // Example: payment.captured, payment.failed
    public string EventType { get; set; } = string.Empty;

    // Store the original JSON for processing and troubleshooting.
    public string Payload { get; set; } = string.Empty;

    // Indicates whether event processing has completed.
    public bool IsProcessed { get; set; } = false;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public string? ProcessingError { get; set; }

    public int ProcessingAttempts { get; set; } = 0;

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? NextAttemptAt { get; set; }
}

