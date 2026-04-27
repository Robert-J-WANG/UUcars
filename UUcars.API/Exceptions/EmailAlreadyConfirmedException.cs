namespace UUcars.API.Exceptions;

public class EmailAlreadyConfirmedException : AppException
{
    public EmailAlreadyConfirmedException()
        : base(StatusCodes.Status409Conflict,
            "Email is already confirmed.")
    {
    }
}