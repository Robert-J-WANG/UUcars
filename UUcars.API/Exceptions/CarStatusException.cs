using UUcars.API.Entities.Enums;

namespace UUcars.API.Exceptions;

// 车辆状态不满足操作要求时抛出
// 比如：只有 Draft 状态才能提交审核，当前是 Published 就抛这个异常
public class CarStatusException : AppException
{
    public CarStatusException(int carId, CarStatus currentStatus, CarStatus requiredStatus)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is in '{currentStatus}' status. Required status: '{requiredStatus}'.")
    {
    }
}