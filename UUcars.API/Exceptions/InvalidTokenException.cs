namespace UUcars.API.Exceptions;

// Token 无效或已过期（邮箱验证和密码重置共用）
// 不区分"Token不存在"和"Token过期"两种情况
// 原因：区分会让攻击者知道Token是否存在，带来信息泄露风险
public class InvalidTokenException : AppException
{
    public InvalidTokenException()
        : base(StatusCodes.Status400BadRequest,
            "The token is invalid or has expired.")
    {
    }
}