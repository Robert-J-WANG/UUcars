namespace UUcars.API.Exceptions;

public class CarNotFoundException:AppException
{
    public CarNotFoundException(int id):base(StatusCodes.Status404NotFound, $"Car with id '{id}' was not found."){}
}