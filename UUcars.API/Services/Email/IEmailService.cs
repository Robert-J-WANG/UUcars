namespace UUcars.API.Services.Email;

// 邮件发送服务接口
// 面向接口设计，方便切换不同的发送实现
// 同时也方便在测试里替换成 Fake 实现
public interface IEmailService
{
    // 发送邮箱验证邮件å
    // email：收件人地址
    // token：验证Token，前端用它调用验证接口
    Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default);

    // 发送密码重置邮件
    Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default);
}