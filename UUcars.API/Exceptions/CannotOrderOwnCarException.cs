namespace UUcars.API.Exceptions;

// 买家尝试购买自己的车
public class CannotOrderOwnCarException : AppException
{
    public CannotOrderOwnCarException()
        : base(StatusCodes.Status400BadRequest,
            "You cannot purchase your own car.")
    {
    }
}