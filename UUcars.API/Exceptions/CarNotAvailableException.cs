namespace UUcars.API.Exceptions;

// 车辆不是 Published 状态，无法下单
// 用专门的异常而不是复用 CarNotFoundException，
// 原因：这辆车是存在的，只是状态不对，语义上不是"找不到"
public class CarNotAvailableException : AppException
{
    public CarNotAvailableException(int carId)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is not available for purchase.")
    {
    }
}