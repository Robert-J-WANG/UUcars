namespace UUcars.API.Exceptions;

// 同一个订单已经评价过了
public class ReviewAlreadyExistsException : AppException
{
    public ReviewAlreadyExistsException()
        : base(StatusCodes.Status409Conflict,
            "This order has already been reviewed.")
    {
    }
}