namespace UUcars.API.Exceptions;

/// <summary>
///     并发冲突异常
///     当两个操作同时修改同一条数据，EF Core 检测到 RowVersion 不匹配时抛出
///     对应 HTTP 409 Conflict
/// </summary>
public class ConcurrencyException : AppException
{
    public ConcurrencyException() : base(StatusCodes.Status409Conflict,
        "The resource was modified by another operation. Please refresh and try again.")
    {
    }
}