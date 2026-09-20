
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Services.Interfaces;
//using TurfBooking.API.Models.Domain;

namespace TurfBooking.API.Services;

public class WebhookEventProcessingService: IWebhookEventProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhookEventProcessingService> _logger;

    private readonly IRefundService _refundService;

    public WebhookEventProcessingService(
        ApplicationDbContext context,
        IRefundService refundService,
        ILogger<WebhookEventProcessingService> logger)
    {
        _context = context;
        _logger = logger;
        _refundService = refundService;
    }


    public async Task ProcessAsync(
        int webhookEventId,
        CancellationToken cancellationToken = default)
    {
        var webhookEvent = await _context.WebhookEvents
            .FirstOrDefaultAsync(
                x => x.Id == webhookEventId,
                cancellationToken);

        if (webhookEvent == null)
        {
            throw new InvalidOperationException(
                $"Webhook event {webhookEventId} was not found.");
        }

        if (webhookEvent.IsProcessed)
            return;

        try
        {
            using var document =
                JsonDocument.Parse(webhookEvent.Payload);

            var root = document.RootElement;

            // A value here means a captured payment needs a refund.
            int? latePaymentRefundId = null;

            switch (webhookEvent.EventType)
            {
                case "payment.captured":
                    latePaymentRefundId =
                        await HandlePaymentCapturedAsync(
                            root,
                            cancellationToken);
                    break;

                case "payment.authorized":
                    await HandlePaymentAuthorizedAsync(
                        root,
                        cancellationToken);
                    break;

                case "payment.failed":
                    await HandlePaymentFailedAsync(
                        root,
                        cancellationToken);
                    break;

                case "refund.processed":
                    await HandleRefundProcessedAsync(
                        root,
                        cancellationToken);
                    break;

                case "refund.failed":
                    await HandleRefundFailedAsync(
                        root,
                        cancellationToken);
                    break;

                default:
                    _logger.LogWarning(
                        "No handler exists for Razorpay event type {EventType}. Event ID: {EventId}",
                        webhookEvent.EventType,
                        webhookEvent.RazorpayEventId);

                    webhookEvent.ProcessingError =
                        $"Handler not implemented for event type: {webhookEvent.EventType}";

                    await _context.SaveChangesAsync(
                        cancellationToken);

                    return;
            }

            // Persist the captured payment and booking state first.
            await _context.SaveChangesAsync(cancellationToken);

            // Refund only after the payment and booking changes are saved.
            if (latePaymentRefundId.HasValue)
            {
                await _refundService.ProcessLatePaymentRefundAsync(
                    latePaymentRefundId.Value,
                    cancellationToken);
            }

            webhookEvent.IsProcessed = true;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            webhookEvent.ProcessingError = null;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            webhookEvent.ProcessingError = ex.Message;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to process Razorpay webhook event {EventId}.",
                webhookEvent.RazorpayEventId);

            throw;
        }
    }



    private async Task<int?> HandlePaymentCapturedAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        var entity = GetPaymentEntity(root);

        var orderId = GetRequiredString(entity, "order_id");
        var paymentId = GetRequiredString(entity, "id");
        var currency = GetRequiredString(entity, "currency");
        var amountInPaise = GetRequiredInt64(entity, "amount");

        if (currency != "INR")
            throw new InvalidOperationException(
                $"Unsupported payment currency: {currency}");

        if (amountInPaise <= 0)
            throw new InvalidOperationException(
                "Captured payment amount must be positive.");

        var payment = await _context.Payments
            .Include(x => x.Booking)
            .ThenInclude(x => x.BookingSlots)
            .FirstOrDefaultAsync(
                x => x.RazorpayOrderId == orderId,
                cancellationToken);

        if (payment == null)
            throw new InvalidOperationException(
                $"Payment for Razorpay order {orderId} was not found.");

        var expectedAmountInPaise = decimal.ToInt64(
            decimal.Round(
                payment.Amount * 100m,
                0,
                MidpointRounding.AwayFromZero));

        if (amountInPaise != expectedAmountInPaise)
            throw new InvalidOperationException(
                "Captured payment amount does not match the booking amount.");

        var booking = payment.Booking;
        var now = DateTime.UtcNow;

        // Never overwrite a completed refund.
        if (payment.Status == PaymentStatus.Refunded)
            return null;

        // A previous attempt may have initiated a refund but failed
        // before the webhook event was marked as processed.
        if (payment.Status == PaymentStatus.RefundPending)
        {
            if (payment.RazorpayPaymentId != paymentId)
                throw new InvalidOperationException(
                    "The captured payment ID does not match the existing payment.");

            if (booking.Status == BookingStatus.Expired ||
                booking.Status == BookingStatus.Cancelled)
            {
                return payment.Id;
            }

            return null;
        }

        if (payment.Status == PaymentStatus.RefundFailed)
        {
            _logger.LogWarning(
                "Payment {PaymentId} has a failed refund and requires review.",
                payment.Id);

            return null;
        }

        if (payment.Status == PaymentStatus.Paid &&
            payment.RazorpayPaymentId != paymentId)
        {
            throw new InvalidOperationException(
                "The captured payment ID does not match the existing payment.");
        }

        payment.RazorpayPaymentId = paymentId;
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt ??= now;
        payment.FailureReason = null;

        booking.PaymentStatus = PaymentStatus.Paid;

        // The booking expired before the payment was captured.
        if (booking.Status == BookingStatus.Pending &&
            booking.ExpiresAt.HasValue &&
            booking.ExpiresAt.Value <= now)
        {
            booking.Status = BookingStatus.Expired;

            foreach (var slot in booking.BookingSlots)
            {
                slot.IsActive = false;
            }

            _logger.LogWarning(
                "Payment {PaymentId} was captured after booking {BookingId} expired. A refund is required.",
                payment.Id,
                booking.Id);

            return payment.Id;
        }

        // Only confirm a booking that is still pending.
        if (booking.Status == BookingStatus.Pending)
        {
            booking.Status = BookingStatus.Confirmed;
            booking.ConfirmedAt ??= now;
            booking.ExpiresAt = null;

            _logger.LogInformation(
                "Booking {BookingId} confirmed from captured payment {PaymentId}.",
                booking.Id,
                payment.Id);

            return null;
        }

        // Cancelled/expired bookings must not be reactivated.
        if (booking.Status == BookingStatus.Expired ||
            booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogWarning(
                "Payment {PaymentId} was captured for booking {BookingId}, which is already {BookingStatus}. A refund is required.",
                payment.Id,
                booking.Id,
                booking.Status);

            return payment.Id;
        }

        // Already confirmed or otherwise not eligible for confirmation.
        return null;
    }


    private async Task HandlePaymentAuthorizedAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        var entity = GetPaymentEntity(root);
        var orderId = GetRequiredString(entity, "order_id");
        var paymentId = GetRequiredString(entity, "id");

        var payment = await _context.Payments
            .FirstOrDefaultAsync(
                x => x.RazorpayOrderId == orderId,
                cancellationToken);

        if (payment == null)
        {
            throw new InvalidOperationException(
                $"No payment record found for Razorpay order {orderId}.");
        }

        if (payment.Status == PaymentStatus.Paid ||
            payment.Status == PaymentStatus.Refunded ||
            payment.Status == PaymentStatus.RefundPending)
        {
            return;
        }

        payment.RazorpayPaymentId = paymentId;
        payment.Status = PaymentStatus.Authorized;
        payment.AuthorizedAt ??= DateTime.UtcNow;
    }

    private async Task HandlePaymentFailedAsync(
        JsonElement root,
        CancellationToken cancellationToken)
    {
        var entity = GetPaymentEntity(root);
        var orderId = GetRequiredString(entity, "order_id");
        var paymentId = GetRequiredString(entity, "id");

        var payment = await _context.Payments
            .FirstOrDefaultAsync(
                x => x.RazorpayOrderId == orderId,
                cancellationToken);

        if (payment == null)
        {
            throw new InvalidOperationException(
                $"No payment record found for Razorpay order {orderId}.");
        }

        // A delayed failure event must never overwrite success.
        if (payment.Status == PaymentStatus.Paid ||
            payment.Status == PaymentStatus.Refunded ||
            payment.Status == PaymentStatus.RefundPending)
        {
            return;
        }

        payment.RazorpayPaymentId = paymentId;
        payment.Status = PaymentStatus.Failed;

        if (entity.TryGetProperty(
                "error_description",
                out var errorDescription) &&
            errorDescription.ValueKind == JsonValueKind.String)
        {
            payment.FailureReason =
                errorDescription.GetString();
        }
        else
        {
            payment.FailureReason = "Razorpay reported payment failure.";
        }
    }

    private static JsonElement GetPaymentEntity(JsonElement root)
    {
        if (root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("payment", out var payment) &&
            payment.TryGetProperty("entity", out var entity) &&
            entity.ValueKind == JsonValueKind.Object)
        {
            return entity;
        }

        throw new InvalidOperationException(
            "Webhook payload does not contain payload.payment.entity.");
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            var value = property.GetString();

            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        throw new InvalidOperationException(
            $"Required payment property '{propertyName}' is missing.");
    }

    private static long GetRequiredInt64(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.TryGetInt64(out var value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Required payment property '{propertyName}' is missing or invalid.");
    }



    //private async Task HandleRefundProcessedAsync(
    //    JsonElement root,
    //    CancellationToken cancellationToken)
    //{
    //    var entity = GetRefundEntity(root);

    //    var refundId = GetRequiredString(entity, "id");
    //    var razorpayPaymentId = GetRequiredString(entity, "payment_id");
    //    var currency = GetRequiredString(entity, "currency");
    //    var amountInPaise = GetRequiredInt64(entity, "amount");

    //    if (currency != "INR")
    //        throw new InvalidOperationException(
    //            $"Unsupported refund currency: {currency}");

    //    var payment = await FindPaymentForRefundAsync(
    //        refundId,
    //        razorpayPaymentId,
    //        cancellationToken);

    //    if (payment == null)
    //        throw new InvalidOperationException(
    //            $"No matching payment was found for Razorpay refund {refundId}.");

    //    var expectedAmountInPaise = decimal.ToInt64(
    //        decimal.Round(
    //            payment.Amount * 100m,
    //            0,
    //            MidpointRounding.AwayFromZero));

    //    if (amountInPaise != expectedAmountInPaise)
    //        throw new InvalidOperationException(
    //            "Refund amount does not match the expected full refund amount.");

    //    // Do not allow an unrelated refund to overwrite an existing one.
    //    if (!string.IsNullOrWhiteSpace(payment.RazorpayRefundId) &&
    //        payment.RazorpayRefundId != refundId)
    //    {
    //        throw new InvalidOperationException(
    //            "The refund ID does not match the refund recorded for this payment.");
    //    }

    //    payment.RazorpayRefundId = refundId;
    //    payment.Status = PaymentStatus.Refunded;
    //    payment.Booking.PaymentStatus = PaymentStatus.Refunded;
    //    payment.RefundedAt ??= DateTime.UtcNow;
    //    payment.FailureReason = null;

    //    _logger.LogInformation(
    //        "Refund {RefundId} processed for payment {PaymentId}.",
    //        refundId,
    //        payment.Id);
    //}


    private async Task HandleRefundProcessedAsync(
JsonElement root,
CancellationToken cancellationToken)
    {
        var entity = GetRefundEntity(root);

        var refundId = GetRequiredString(entity, "id");
        var razorpayPaymentId = GetRequiredString(entity, "payment_id");
        var currency = GetRequiredString(entity, "currency");
        var amountInPaise = GetRequiredInt64(entity, "amount");

        if (currency != "INR")
            throw new InvalidOperationException(
                $"Unsupported refund currency: {currency}");

        var payment = await FindPaymentForRefundAsync(
            refundId,
            razorpayPaymentId,
            cancellationToken);

        if (payment == null)
            throw new InvalidOperationException(
                $"No matching payment was found for Razorpay refund {refundId}.");

        // Do not allow a different refund to overwrite the recorded refund.
        if (!string.IsNullOrWhiteSpace(payment.RazorpayRefundId) &&
            payment.RazorpayRefundId != refundId)
        {
            throw new InvalidOperationException(
                "The refund ID does not match the refund recorded for this payment.");
        }

        // Cancellation refunds may be partial. Late-payment refunds default to full.
        var expectedRefundAmount = payment.RefundAmount ?? payment.Amount;

        var expectedAmountInPaise = decimal.ToInt64(
            decimal.Round(
                expectedRefundAmount * 100m,
                0,
                MidpointRounding.AwayFromZero));

        if (amountInPaise != expectedAmountInPaise)
            throw new InvalidOperationException(
                $"Refund amount mismatch. Expected {expectedAmountInPaise} paise, " +
                $"received {amountInPaise} paise.");

        // Duplicate refund.processed event: do not update timestamps unnecessarily.
        if (payment.Status == PaymentStatus.Refunded)
        {
            _logger.LogInformation(
                "Refund {RefundId} was already processed for payment {PaymentId}.",
                refundId,
                payment.Id);

            return;
        }

        payment.RazorpayRefundId = refundId;
        payment.Status = PaymentStatus.Refunded;
        payment.Booking.PaymentStatus = PaymentStatus.Refunded;
        payment.RefundedAt ??= DateTime.UtcNow;
        payment.FailureReason = null;

        _logger.LogInformation(
            "Refund {RefundId} processed for payment {PaymentId}.",
            refundId,
            payment.Id);

    }

    //private async Task HandleRefundFailedAsync(
    //    JsonElement root,
    //    CancellationToken cancellationToken)
    //{
    //    var entity = GetRefundEntity(root);

    //    var refundId = GetRequiredString(entity, "id");
    //    var razorpayPaymentId = GetRequiredString(entity, "payment_id");

    //    var payment = await FindPaymentForRefundAsync(
    //        refundId,
    //        razorpayPaymentId,
    //        cancellationToken);

    //    if (payment == null)
    //        throw new InvalidOperationException(
    //            $"No matching payment was found for failed refund {refundId}.");

    //    // A delayed failure event must never downgrade a completed refund.
    //    if (payment.Status == PaymentStatus.Refunded)
    //    {
    //        _logger.LogWarning(
    //            "Ignoring refund.failed event for already-refunded payment {PaymentId}.",
    //            payment.Id);

    //        return;
    //    }

    //    if (!string.IsNullOrWhiteSpace(payment.RazorpayRefundId) &&
    //        payment.RazorpayRefundId != refundId)
    //    {
    //        throw new InvalidOperationException(
    //            "The refund ID does not match the refund recorded for this payment.");
    //    }

    //    payment.RazorpayRefundId = refundId;
    //    payment.Status = PaymentStatus.RefundFailed;
    //    payment.Booking.PaymentStatus = PaymentStatus.RefundFailed;
    //    payment.FailureReason = TryGetOptionalString(
    //        entity,
    //        "error_description") ?? "Razorpay refund failed.";

    //    _logger.LogWarning(
    //        "Refund {RefundId} failed for payment {PaymentId}: {Reason}",
    //        refundId,
    //        payment.Id,
    //        payment.FailureReason);
    //}

    private async Task HandleRefundFailedAsync(
JsonElement root,
CancellationToken cancellationToken)
    {
        var entity = GetRefundEntity(root);

        var refundId = GetRequiredString(entity, "id");
        var razorpayPaymentId = GetRequiredString(entity, "payment_id");
        var currency = GetRequiredString(entity, "currency");
        var amountInPaise = GetRequiredInt64(entity, "amount");

        if (currency != "INR")
            throw new InvalidOperationException(
                $"Unsupported refund currency: {currency}");

        var payment = await FindPaymentForRefundAsync(
            refundId,
            razorpayPaymentId,
            cancellationToken);

        if (payment == null)
            throw new InvalidOperationException(
                $"No matching payment was found for failed refund {refundId}.");

        if (!string.IsNullOrWhiteSpace(payment.RazorpayRefundId) &&
            payment.RazorpayRefundId != refundId)
        {
            throw new InvalidOperationException(
                "The refund ID does not match the refund recorded for this payment.");
        }

        var expectedRefundAmount = payment.RefundAmount ?? payment.Amount;

        var expectedAmountInPaise = decimal.ToInt64(
            decimal.Round(
                expectedRefundAmount * 100m,
                0,
                MidpointRounding.AwayFromZero));

        if (amountInPaise != expectedAmountInPaise)
            throw new InvalidOperationException(
                $"Refund amount mismatch. Expected {expectedAmountInPaise} paise, " +
                $"received {amountInPaise} paise.");

        // A delayed failure event must never downgrade a successful refund.
        if (payment.Status == PaymentStatus.Refunded)
        {
            _logger.LogWarning(
                "Ignoring refund.failed event for already-refunded payment {PaymentId}.",
                payment.Id);

            return;
        }

        payment.RazorpayRefundId = refundId;
        payment.Status = PaymentStatus.RefundFailed;
        payment.Booking.PaymentStatus = PaymentStatus.RefundFailed;
        payment.FailureReason = TryGetOptionalString(
            entity,
            "error_description") ?? "Razorpay refund failed.";

        _logger.LogWarning(
            "Refund {RefundId} failed for payment {PaymentId}: {Reason}",
            refundId,
            payment.Id,
            payment.FailureReason);

    }

    private static JsonElement GetRefundEntity(JsonElement root)
    {
        if (root.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("refund", out var refund) &&
            refund.TryGetProperty("entity", out var entity))
        {
            return entity;
        }

        throw new InvalidOperationException(
            "Razorpay refund entity was missing from the webhook payload.");
    }

    private async Task<Models.Domain.Payment?> FindPaymentForRefundAsync(
        string refundId,
        string razorpayPaymentId,
        CancellationToken cancellationToken)
    {
        // First, find the exact refund if its ID is already stored.
        var payment = await _context.Payments
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(
                x => x.RazorpayRefundId == refundId,
                cancellationToken);

        if (payment != null)
            return payment;

        // Fallback: the refund webhook arrived before the API response
        // stored the refund ID. Only match a payment awaiting a refund.
        return await _context.Payments
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(
                x => x.RazorpayPaymentId == razorpayPaymentId &&
                     x.Status == PaymentStatus.RefundPending,
                cancellationToken);
    }

    private static string? TryGetOptionalString(
        JsonElement element,
        string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

}
