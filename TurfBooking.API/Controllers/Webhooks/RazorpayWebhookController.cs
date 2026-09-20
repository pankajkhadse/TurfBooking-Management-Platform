using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurfBooking.API.Data;
using TurfBooking.API.Data.Configurations;
//using TurfBooking.API.Models.Configuration;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Settings;

namespace TurfBooking.API.Controllers.Webhooks;

[ApiController]
[Route("api/webhooks/razorpay")]
public class RazorpayWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RazorpaySettings _razorpaySettings;
    private readonly ILogger<RazorpayWebhookController> _logger;

    public RazorpayWebhookController(
        ApplicationDbContext context,
        IOptions<RazorpaySettings> razorpaySettings,
        ILogger<RazorpayWebhookController> logger)
    {
        _context = context;
        _razorpaySettings = razorpaySettings.Value;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        CancellationToken cancellationToken)
    {
        // 1. Read the exact raw request body.
        Request.EnableBuffering();

        string rawBody;

        using (var reader = new StreamReader(
            Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(
                cancellationToken);
        }

        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return BadRequest("Webhook body is empty.");
        }

        // 2. Get the Razorpay signature.
        if (!Request.Headers.TryGetValue(
                "X-Razorpay-Signature",
                out var signatureHeader))
        {
            return Unauthorized(
                "Missing Razorpay signature.");
        }

        var receivedSignature = signatureHeader.ToString();

        if (string.IsNullOrWhiteSpace(
                _razorpaySettings.WebhookSecret))
        {
            _logger.LogError(
                "Razorpay webhook secret is not configured.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Webhook configuration error.");
        }

        // 3. Verify the signature BEFORE trusting the event.
        var isValid = VerifyWebhookSignature(
            rawBody,
            receivedSignature,
            _razorpaySettings.WebhookSecret);

        if (!isValid)
        {
            _logger.LogWarning(
                "Invalid Razorpay webhook signature.");

            return Unauthorized(
                "Invalid webhook signature.");
        }

        // 4. Read the unique event ID from the request header.
        if (!Request.Headers.TryGetValue(
                "x-razorpay-event-id",
                out var eventIdHeader))
        {
            return BadRequest(
                "Missing Razorpay event ID.");
        }

        var razorpayEventId = eventIdHeader.ToString().Trim();

        if (string.IsNullOrWhiteSpace(razorpayEventId) ||
            razorpayEventId.Length > 255)
        {
            return BadRequest(
                "Invalid Razorpay event ID.");
        }

        // 5. Extract the event type from the JSON payload.
        string eventType;

        try
        {
            using var document = JsonDocument.Parse(rawBody);

            if (!document.RootElement.TryGetProperty(
                    "event",
                    out var eventElement) ||
                eventElement.ValueKind != JsonValueKind.String)
            {
                return BadRequest(
                    "Missing or invalid event type.");
            }

            eventType = eventElement.GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(eventType) ||
                eventType.Length > 100)
            {
                return BadRequest(
                    "Invalid event type.");
            }
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON payload.");
        }

        // 6. Check whether this event has already been stored.
        var alreadyExists = await _context.WebhookEvents
            .AnyAsync(
                x => x.RazorpayEventId == razorpayEventId,
                cancellationToken);

        if (alreadyExists)
        {
            _logger.LogInformation(
                "Duplicate Razorpay webhook received. Event ID: {EventId}",
                razorpayEventId);

            return Ok(new
            {
                message = "Webhook event already received."
            });
        }

        // 7. Save the event. Payment processing comes later.
        var webhookEvent = new WebhookEvent
        {
            RazorpayEventId = razorpayEventId,
            EventType = eventType,
            Payload = rawBody,
            IsProcessed = false,
            ReceivedAt = DateTime.UtcNow
        };

        _context.WebhookEvents.Add(webhookEvent);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (IsDuplicateKeyException(ex))
        {
            // Two identical deliveries can arrive concurrently.
            // The unique database index protects against both
            // requests inserting the same event.
            _logger.LogInformation(
                "Duplicate Razorpay webhook insert prevented. Event ID: {EventId}",
                razorpayEventId);

            return Ok(new
            {
                message = "Webhook event already received."
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to save Razorpay webhook event {EventId}.",
                razorpayEventId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "Unable to save webhook event.");
        }

        _logger.LogInformation(
            "Razorpay webhook event stored. Event ID: {EventId}, Type: {EventType}",
            razorpayEventId,
            eventType);

        return Ok(new
        {
            message = "Webhook event stored successfully.",
            eventId = razorpayEventId
        });
    }

    private static bool VerifyWebhookSignature(
        string rawBody,
        string receivedSignature,
        string webhookSecret)
    {
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(webhookSecret));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(rawBody));

        var generatedSignature =
            Convert.ToHexString(hash).ToLowerInvariant();

        Console.WriteLine(generatedSignature);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(generatedSignature),
            Encoding.UTF8.GetBytes(receivedSignature));
    }

    private static bool IsDuplicateKeyException(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601 ||
                sqlException.Number == 2627);
    }
}