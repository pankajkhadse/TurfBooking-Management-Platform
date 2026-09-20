using TurfBooking.API.Enums;

namespace TurfBooking.API.Models.Domain;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    // Razorpay order created for this booking.
    public string RazorpayOrderId { get; set; } = string.Empty;

    // Razorpay payment ID generated after payment attempt.
    public string? RazorpayPaymentId { get; set; }

    // Signature returned by Razorpay Checkout.
    public string? RazorpaySignature { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // When Razorpay authorizes the payment.
    public DateTime? AuthorizedAt { get; set; }

    // When the payment is successfully captured.
    // Your existing PaidAt is retained so we don't break
    // your current code.
    public DateTime? PaidAt { get; set; }

    // Razorpay refund ID, when a refund is created.
    public string? RazorpayRefundId { get; set; }

    // When refund processing was initiated.
    public DateTime? RefundInitiatedAt { get; set; }

    // When refund was successfully completed.
    public DateTime? RefundedAt { get; set; }

    // Useful for payment/refund failure investigation.
    public string? FailureReason { get; set; }

    public decimal? RefundAmount { get; set; }
}