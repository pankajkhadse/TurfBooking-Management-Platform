using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Services.Interfaces;
using TurfBooking.API.Settings;

namespace TurfBooking.API.Services.Implementations;

public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RazorpaySettings _settings;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<RazorpaySettings> settings,
        ILogger<RefundService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ProcessLatePaymentRefundAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment == null)
            throw new InvalidOperationException($"Payment {paymentId} was not found.");

        if (payment.Status == PaymentStatus.Refunded)
            return;

        if (payment.Status == PaymentStatus.RefundPending &&
            !string.IsNullOrWhiteSpace(payment.RazorpayRefundId))
            return;

        if (payment.Status != PaymentStatus.Paid &&
            payment.Status != PaymentStatus.RefundPending)
            throw new InvalidOperationException("Only captured payments can be refunded.");

        if (string.IsNullOrWhiteSpace(payment.RazorpayPaymentId))
            throw new InvalidOperationException("The Razorpay payment ID is missing.");

        if (payment.Booking.Status != BookingStatus.Expired &&
            payment.Booking.Status != BookingStatus.Cancelled)
            throw new InvalidOperationException(
                "This refund flow is only for expired or cancelled bookings.");

        await SubmitRefundAsync(
            payment,
            payment.Amount,
            $"late-payment-refund-{payment.Id}",
            "Payment received after booking expired",
            cancellationToken);
    }

    public async Task ProcessCancellationRefundAsync(
        int paymentId,
        decimal refundAmount,
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(
                p => p.Id == paymentId && p.BookingId == bookingId,
                cancellationToken);

        if (payment == null)
            throw new InvalidOperationException("Payment not found for this booking.");

        if (payment.Status == PaymentStatus.Refunded)
            return;

        if (payment.Status == PaymentStatus.RefundPending &&
            !string.IsNullOrWhiteSpace(payment.RazorpayRefundId))
            return;

        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException(
                "Only paid bookings can receive a cancellation refund.");

        if (payment.Booking.Status != BookingStatus.Cancelled)
            throw new InvalidOperationException(
                "Cancellation refund requires a cancelled booking.");

        if (refundAmount <= 0m || refundAmount > payment.Amount)
            throw new InvalidOperationException(
                "Refund amount must be greater than zero and no more than the amount paid.");

        await SubmitRefundAsync(
            payment,
            refundAmount,
            $"cancellation-refund-{payment.Id}",
            "Customer cancelled booking",
            cancellationToken);
    }

    private async Task SubmitRefundAsync(
        TurfBooking.API.Models.Domain.Payment payment,
        decimal refundAmount,
        string idempotencyKey,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payment.RazorpayPaymentId))
            throw new InvalidOperationException("Razorpay payment ID is missing.");

        if (string.IsNullOrWhiteSpace(_settings.KeyId) ||
            string.IsNullOrWhiteSpace(_settings.KeySecret))
            throw new InvalidOperationException("Razorpay API credentials are not configured.");

        // Save before the external call. Retries must reuse the same idempotency key.
        payment.RefundAmount = refundAmount;
        payment.Status = PaymentStatus.RefundPending;
        payment.Booking.PaymentStatus = PaymentStatus.RefundPending;
        payment.RefundInitiatedAt ??= DateTime.UtcNow;
        payment.FailureReason = null;
        await _context.SaveChangesAsync(cancellationToken);

        var amountInPaise = decimal.ToInt64(decimal.Round(
            refundAmount * 100m, 0, MidpointRounding.AwayFromZero));

        var requestBody = JsonSerializer.Serialize(new
        {
            amount = amountInPaise,
            notes = new Dictionary<string, string>
            {
                ["reason"] = reason,
                ["booking_id"] = payment.BookingId.ToString()
            }
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.razorpay.com/v1/payments/" +
            $"{Uri.EscapeDataString(payment.RazorpayPaymentId)}/refund");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{_settings.KeyId}:{_settings.KeySecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("X-Refund-Idempotency", idempotencyKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = $"Razorpay refund API returned {(int)response.StatusCode}: {responseBody}";
            payment.FailureReason = error;

            if ((int)response.StatusCode >= 400 &&
                (int)response.StatusCode < 500 &&
                response.StatusCode != HttpStatusCode.Conflict)
            {
                payment.Status = PaymentStatus.RefundFailed;
                payment.Booking.PaymentStatus = PaymentStatus.RefundFailed;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(
                "Refund API request failed for payment {PaymentId}. HTTP {StatusCode}.",
                payment.Id,
                (int)response.StatusCode);
            throw new HttpRequestException(error);
        }

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var refundId = root.TryGetProperty("id", out var idElement)
            ? idElement.GetString()
            : null;
        var refundStatus = root.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(refundId))
            throw new InvalidOperationException(
                "Razorpay returned a refund response without an ID.");

        payment.RazorpayRefundId = refundId;
        payment.FailureReason = null;

        switch (refundStatus?.ToLowerInvariant())
        {
            case "processed":
                payment.Status = PaymentStatus.Refunded;
                payment.Booking.PaymentStatus = PaymentStatus.Refunded;
                payment.RefundedAt = DateTime.UtcNow;
                break;
            case "pending":
                payment.Status = PaymentStatus.RefundPending;
                payment.Booking.PaymentStatus = PaymentStatus.RefundPending;
                break;
            case "failed":
                payment.Status = PaymentStatus.RefundFailed;
                payment.Booking.PaymentStatus = PaymentStatus.RefundFailed;
                payment.FailureReason = "Razorpay reported that the refund failed.";
                break;
            default:
                payment.Status = PaymentStatus.RefundPending;
                payment.Booking.PaymentStatus = PaymentStatus.RefundPending;
                payment.FailureReason = $"Unexpected Razorpay refund status: {refundStatus}";
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Refund {RefundId} created for payment {PaymentId}; status {RefundStatus}.",
            refundId, payment.Id, payment.Status);
    }
}
