namespace TurfBooking.API.Settings;

public class RazorpaySettings
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = string.Empty;

    public string KeySecret { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}