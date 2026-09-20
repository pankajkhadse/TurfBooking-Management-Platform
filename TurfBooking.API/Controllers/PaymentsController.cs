using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurfBooking.API.Models.Requests.Payment;
using TurfBooking.API.Services.Interfaces;

namespace TurfBooking.API.Controllers;

[Route("api/payments")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreatePaymentOrderRequest request)
    {
        var result = await _paymentService.CreateOrderAsync(request);

        return Ok(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment(
    [FromBody] VerifyPaymentRequest request)
    {
        var result = await _paymentService.VerifyPaymentAsync(request);

        return Ok(result);
    }

}