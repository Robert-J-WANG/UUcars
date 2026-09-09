namespace UUcars.API.Exceptions;

public class InvalidGoogleIdTokenException : AppException
{
    public InvalidGoogleIdTokenException()
        : base(
            StatusCodes.Status401Unauthorized,
            "Google authentication failed.")
    {
    }
}