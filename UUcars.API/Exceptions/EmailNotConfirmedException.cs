namespace UUcars.API.Exceptions;

// 邮箱未验证时尝试登录
// 为什么用 403 而不是 401？
// 401（Unauthorized）：没有提供任何身份凭证，服务器不知道你是谁
// 403（Forbidden）：  身份已确认（密码正确），但这个账号当前不允许访问
// 未验证邮箱属于后者：密码是对的，只是账号还没完成激活
// 前端收到 403 就知道要显示"请验证邮箱"的提示，而不是"密码错误"
public class EmailNotConfirmedException : AppException
{
    public EmailNotConfirmedException()
        : base(StatusCodes.Status403Forbidden,
            "Please verify your email address before logging in.")
    {
    }
}