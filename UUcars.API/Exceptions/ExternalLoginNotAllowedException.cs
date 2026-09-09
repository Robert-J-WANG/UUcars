namespace UUcars.API.Exceptions;

public class ExternalLoginNotAllowedException : AppException
{
    public ExternalLoginNotAllowedException(string message)
        : base(StatusCodes.Status403Forbidden, message)
    {
    }
}