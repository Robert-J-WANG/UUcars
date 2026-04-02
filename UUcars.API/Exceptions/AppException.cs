namespace UUcars.API.Exceptions;

// 业务异常基类
// Service 层抛出继承自这个类的异常，
// 中间件统一捕获并转换成对应的 HTTP 响应
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}