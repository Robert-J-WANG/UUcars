namespace UUcars.API.Exceptions;

// 订单未完成，不能评价
public class OrderNotCompletedException : AppException
{
    public OrderNotCompletedException()
        : base(StatusCodes.Status409Conflict,
            "You can only review a completed order.")
    {
    }
}