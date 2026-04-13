namespace UUcars.API.Exceptions;

public class UserAlreadyExistsException :AppException
{
    public UserAlreadyExistsException(string email) : base(StatusCodes.Status409Conflict,$"Email '{email}' is already registered."){}
}