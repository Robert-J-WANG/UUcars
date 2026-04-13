using UUcars.API.Entities.Enums;

namespace UUcars.API.Exceptions;

// 订单状态不满足操作要求时抛出
// 比如：只有 Pending 状态才能取消，Completed 的订单不能取消
public class OrderStatusException : AppException
{
    public OrderStatusException(int orderId, OrderStatus currentStatus, OrderStatus requiredStatus)
        : base(StatusCodes.Status409Conflict,
            $"Order '{orderId}' is in '{currentStatus}' status. Required status: '{requiredStatus}'.")
    {
    }
}