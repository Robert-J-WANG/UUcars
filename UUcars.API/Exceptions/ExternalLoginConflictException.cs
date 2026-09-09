namespace UUcars.API.Exceptions;

public class ExternalLoginConflictException : AppException
{
    public ExternalLoginConflictException()
        : base(
            StatusCodes.Status409Conflict,
            "This email is already registered and cannot be linked automatically.")
    {
    }
}