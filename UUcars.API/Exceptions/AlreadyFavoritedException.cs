namespace UUcars.API.Exceptions;

// 用户尝试重复收藏同一辆车时抛出
public class AlreadyFavoritedException : AppException
{
    public AlreadyFavoritedException(int carId)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is already in your favorites.")
    {
    }
}