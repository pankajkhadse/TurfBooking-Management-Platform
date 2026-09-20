namespace TurfBooking.API.Settings;

public class BookingSettings
{
    public const string SectionName = "BookingSettings";

    public int PaymentExpiryMinutes { get; set; } = 10;
}