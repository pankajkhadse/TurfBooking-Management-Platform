namespace TurfBooking.API.Enums;

public enum PaymentStatus
{
    Pending = 1,

    // Payment was successfully authorized by Razorpay
    // but has not necessarily been captured yet.
    Authorized = 5,

    // Payment has been captured successfully.
    Paid = 2,

    // Payment attempt failed.
    Failed = 3,

    // Refund has been requested and is being processed.
    RefundPending = 6,

    // Payment has been completely refunded.
    Refunded = 4,

    // Refund attempt failed and needs reconciliation.
    RefundFailed = 7
}