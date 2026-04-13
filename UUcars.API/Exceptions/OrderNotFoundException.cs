namespace UUcars.API.Exceptions;

public class OrderNotFoundException : AppException
{
    public OrderNotFoundException(int id)
        : base(StatusCodes.Status404NotFound, $"Order with id '{id}' was not found.")
    {
    }
}