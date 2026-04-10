namespace UUcars.API.Exceptions;

public class ForbiddenException:AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(StatusCodes.Status403Forbidden, message)
    {
    }
}