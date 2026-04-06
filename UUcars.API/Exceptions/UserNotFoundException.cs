namespace UUcars.API.Exceptions;

public class UserNotFoundException:AppException
{
    public UserNotFoundException(int id)
        : base(StatusCodes.Status404NotFound, $"User with id '{id}' was not found.")
    {
    }
}