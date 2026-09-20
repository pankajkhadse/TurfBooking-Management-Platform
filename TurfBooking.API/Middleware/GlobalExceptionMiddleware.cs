using System.Net;
using System.Text.Json;
using TurfBooking.API.Exceptions;
using TurfBooking.API.Models.Common;

namespace TurfBooking.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case BadRequestException badRequest:
                response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = badRequest.Message;
                response.Errors = badRequest.Errors;
                break;

            case UnauthorizedException unauthorized:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = unauthorized.Message;
                break;

            case ForbiddenException forbidden:
                response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = forbidden.Message;
                break;

            case NotFoundException notFound:
                response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = notFound.Message;
                break;

            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An unexpected error occurred.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}