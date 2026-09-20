namespace TurfBooking.API.Exceptions;

public class BadRequestException : Exception
{
    public List<string>? Errors { get; }

    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, List<string> errors)
        : base(message)
    {
        Errors = errors;
    }
}