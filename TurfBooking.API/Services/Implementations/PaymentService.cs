using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using TurfBooking.API.Data;
using TurfBooking.API.Enums;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Domain;
using TurfBooking.API.Models.Requests.Payment;
using TurfBooking.API.Models.Responses.Payment;
using TurfBooking.API.Services.Interfaces;
using TurfBooking.API.Settings;
using System.Security.Cryptography;
using System.Text;

namespace TurfBooking.API.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly RazorpaySettings _razorpaySettings;

    private readonly IBookingLifecycleService _bookingLifecycleService;

    public PaymentService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IOptions<RazorpaySettings> razorpayOptions,
        IBookingLifecycleService bookingLifecycleService)
    {
        _context = context;
        _currentUser = currentUser;
        _razorpaySettings = razorpayOptions.Value;
        _bookingLifecycleService = bookingLifecycleService;
    }

    public async Task<ApiResponse<PaymentOrderResponse>> CreateOrderAsync(
        CreatePaymentOrderRequest request)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException(
                "User is not authenticated.");

        var booking = await _context.Bookings
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x =>
                x.Id == request.BookingId &&
                x.UserId == userId);

        if (booking == null)
            throw new NotFoundException(
                "Booking not found.");

        if (booking.Status != BookingStatus.Pending)
            throw new BadRequestException(
                "Only pending bookings can be paid.");

        if (booking.ExpiresAt.HasValue &&
    booking.ExpiresAt.Value <= DateTime.UtcNow)
        {
            _bookingLifecycleService.ExpireBooking(booking);

            await _context.SaveChangesAsync();

            throw new BadRequestException(
                "The booking has expired. Please select the slot again.");
        }

        if (booking.Payment != null)
        {
            throw new BadRequestException(
                "Payment order has already been created for this booking.");
        }

        if (booking.Amount <= 0)
            throw new BadRequestException(
                "Invalid booking amount.");

        if (string.IsNullOrWhiteSpace(_razorpaySettings.KeyId) ||
            string.IsNullOrWhiteSpace(_razorpaySettings.KeySecret))
        {
            throw new BadRequestException(
                "Razorpay configuration is missing.");
        }

        var client = new RazorpayClient(
            _razorpaySettings.KeyId,
            _razorpaySettings.KeySecret);

        var options = new Dictionary<string, object>
        {
            { "amount", Convert.ToInt64(booking.Amount * 100) },
            { "currency", "INR" },
            { "receipt", $"booking_{booking.Id}" }
        };

        Order order = client.Order.Create(options);

        var payment = new Models.Domain.Payment
        {
            BookingId = booking.Id,
            RazorpayOrderId = order["id"].ToString()!,
            Amount = booking.Amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        var response = new PaymentOrderResponse
        {
            BookingId = booking.Id,
            PaymentId = payment.Id,
            RazorpayOrderId = payment.RazorpayOrderId,
            RazorpayKeyId = _razorpaySettings.KeyId,
            Amount = payment.Amount,
            Currency = "INR"
        };

        return ApiResponse<PaymentOrderResponse>.SuccessResponse(
            response,
            "Payment order created successfully.");
    }


    public async Task<ApiResponse<PaymentVerificationResponse>> VerifyPaymentAsync(
        VerifyPaymentRequest request)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException(
                "User is not authenticated.");

        if (string.IsNullOrWhiteSpace(request.RazorpayPaymentId))
            throw new BadRequestException(
                "Razorpay payment ID is required.");

        if (string.IsNullOrWhiteSpace(request.RazorpaySignature))
            throw new BadRequestException(
                "Razorpay signature is required.");

        var booking = await _context.Bookings
            .Include(x => x.Payment)
            .Include(x => x.BookingSlots)
            .FirstOrDefaultAsync(x =>
                x.Id == request.BookingId &&
                x.UserId == userId);

        if (booking == null)
            throw new NotFoundException(
                "Booking not found.");

        if (booking.Payment == null)
            throw new BadRequestException(
                "Payment order was not created for this booking.");

        var payment = booking.Payment;

        var razorpayOrderId = payment.RazorpayOrderId;

        if (string.IsNullOrWhiteSpace(razorpayOrderId))
            throw new BadRequestException(
                "Razorpay order ID is missing.");

        // Verify the Checkout signature before changing payment state
        // or saving the signature to the database.
        var payload =
            $"{razorpayOrderId}|{request.RazorpayPaymentId}";

        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_razorpaySettings.KeySecret));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload));

        var generatedSignature =
            Convert.ToHexString(hash).ToLowerInvariant();

        byte[] generatedBytes;
        byte[] receivedBytes;

        try
        {
            generatedBytes = Convert.FromHexString(generatedSignature);
            receivedBytes = Convert.FromHexString(
                request.RazorpaySignature);
        }
        catch (FormatException)
        {
            throw new BadRequestException(
                "Payment verification failed.");
        }

        if (generatedBytes.Length != receivedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(
                generatedBytes,
                receivedBytes))
        {
            // Do not overwrite a previously successful payment
            // when an invalid verification request is received.
            if (payment.Status != PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
            }

            throw new BadRequestException(
                "Payment verification failed.");
        }

        // The signature is valid from this point onward.

        // Idempotency: the webhook may have confirmed the booking
        // before the frontend called this verification endpoint.
        if (payment.Status == PaymentStatus.Paid &&
            booking.Status == BookingStatus.Confirmed)
        {
            if (!string.IsNullOrWhiteSpace(payment.RazorpayPaymentId) &&
                payment.RazorpayPaymentId != request.RazorpayPaymentId)
            {
                throw new BadRequestException(
                    "Payment ID does not match the confirmed payment.");
            }

            payment.RazorpayPaymentId = request.RazorpayPaymentId;
            payment.RazorpaySignature = request.RazorpaySignature;

            await _context.SaveChangesAsync();

            return ApiResponse<PaymentVerificationResponse>.SuccessResponse(
                new PaymentVerificationResponse
                {
                    BookingId = booking.Id,
                    PaymentId = payment.Id,
                    RazorpayPaymentId = payment.RazorpayPaymentId,
                    Status = "Success",
                    Message = "Payment was already verified and confirmed."
                },
                "Payment already verified.");
        }

        // Do not allow an expired, cancelled, or otherwise completed
        // booking to be confirmed by a late Checkout verification.
        if (booking.Status != BookingStatus.Pending)
            throw new BadRequestException(
                "This booking is no longer pending.");

        if (booking.ExpiresAt.HasValue &&
            booking.ExpiresAt.Value <= DateTime.UtcNow)
        {
            _bookingLifecycleService.ExpireBooking(booking);

            await _context.SaveChangesAsync();

            throw new BadRequestException(
                "The booking has expired. Please select the slot again.");
        }

        if (payment.Status == PaymentStatus.RefundPending ||
            payment.Status == PaymentStatus.Refunded ||
            payment.Status == PaymentStatus.RefundFailed)
        {
            throw new BadRequestException(
                "This payment is in a refund state and cannot confirm the booking.");
        }

        // Store the verified Checkout details.
        var now = DateTime.UtcNow;

        payment.RazorpayPaymentId = request.RazorpayPaymentId;
        payment.RazorpaySignature = request.RazorpaySignature;
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt ??= now;
        payment.FailureReason = null;

        booking.Status = BookingStatus.Confirmed;
        booking.PaymentStatus = PaymentStatus.Paid;
        booking.ConfirmedAt ??= now;
        booking.ExpiresAt = null;

        await _context.SaveChangesAsync();

        var response = new PaymentVerificationResponse
        {
            BookingId = booking.Id,
            PaymentId = payment.Id,
            RazorpayPaymentId = payment.RazorpayPaymentId,
            Status = "Success",
            Message = "Payment verified and booking confirmed."
        };

        return ApiResponse<PaymentVerificationResponse>.SuccessResponse(
            response,
            "Payment verified successfully. Booking confirmed.");
    }

}