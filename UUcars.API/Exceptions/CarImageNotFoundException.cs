namespace UUcars.API.Exceptions;

public class CarImageNotFoundException : AppException
{
    public CarImageNotFoundException(int imageId)
        : base(StatusCodes.Status404NotFound, $"Car image with id '{imageId}' was not found.")
    {
    }
}