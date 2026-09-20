using TurfBooking.API.Models.Common;
using TurfBooking.API.Models.Requests.Payment;
using TurfBooking.API.Models.Responses.Payment;

namespace TurfBooking.API.Services.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<PaymentOrderResponse>> CreateOrderAsync(
        CreatePaymentOrderRequest request);

    Task<ApiResponse<PaymentVerificationResponse>> VerifyPaymentAsync(
        VerifyPaymentRequest request);
}