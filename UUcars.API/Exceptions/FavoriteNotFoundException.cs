namespace UUcars.API.Exceptions;

// 用户尝试取消一个不存在的收藏时抛出
public class FavoriteNotFoundException : AppException
{
    public FavoriteNotFoundException(int carId)
        : base(StatusCodes.Status404NotFound,
            $"Car '{carId}' is not in your favorites.")
    {
    }
}