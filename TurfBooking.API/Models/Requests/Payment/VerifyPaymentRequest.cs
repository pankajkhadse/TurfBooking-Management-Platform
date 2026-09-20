namespace TurfBooking.API.Models.Requests.Payment;

public class VerifyPaymentRequest
{
    public int BookingId { get; set; }

    public string RazorpayPaymentId { get; set; } = string.Empty;

    public string RazorpaySignature { get; set; } = string.Empty;
}