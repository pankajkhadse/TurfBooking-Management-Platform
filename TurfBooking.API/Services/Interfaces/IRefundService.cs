namespace TurfBooking.API.Services.Interfaces;

public interface IRefundService
{
    Task ProcessLatePaymentRefundAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task ProcessCancellationRefundAsync(
        int paymentId,
        decimal refundAmount,
        int bookingId,
        CancellationToken cancellationToken = default);
}
