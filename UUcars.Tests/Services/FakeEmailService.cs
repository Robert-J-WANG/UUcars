using UUcars.API.Services.Email;

namespace UUcars.Tests.Services;

// 测试用的假邮件服务，不发真实邮件
// 记录发送记录，供测试里断言"邮件是否发送、发给了谁、Token是什么"
public class FakeEmailService : IEmailService
{
    public List<(string Email, string Token)> SentVerificationEmails { get; } = [];
    public List<(string Email, string Token)> SentPasswordResetEmails { get; } = [];

    public Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        SentVerificationEmails.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        SentPasswordResetEmails.Add((email, token));
        return Task.CompletedTask;
    }
}