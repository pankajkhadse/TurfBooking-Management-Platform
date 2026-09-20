namespace TurfBooking.API.Models.Responses.Payment;

public class PaymentOrderResponse
{
    public int BookingId { get; set; }

    public int PaymentId { get; set; }

    public string RazorpayOrderId { get; set; } = string.Empty;

    public string RazorpayKeyId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";
}