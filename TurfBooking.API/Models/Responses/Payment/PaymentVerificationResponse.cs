namespace TurfBooking.API.Models.Responses.Payment;

public class PaymentVerificationResponse
{
    public int BookingId { get; set; }

    public int PaymentId { get; set; }

    public string RazorpayPaymentId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}