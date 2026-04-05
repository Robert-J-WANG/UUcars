namespace UUcars.API.Exceptions;

public class InvalidCredentialsException:AppException
{
    public InvalidCredentialsException()
        : base(StatusCodes.Status401Unauthorized, "Invalid email or password.")
    {
    }
}