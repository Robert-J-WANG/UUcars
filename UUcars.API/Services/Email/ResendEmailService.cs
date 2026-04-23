using Microsoft.Extensions.Options;
using Resend;
using UUcars.API.Configurations;

namespace UUcars.API.Services.Email;

public class ResendEmailService : IEmailService
{
    // 获取email参数的配置
    private readonly EmailSettings _emailSettings;

    private readonly ILogger<ResendEmailService> _logger;

    // resend的核心对象
    private readonly IResend _resend;

    public ResendEmailService(
        IResend resend,
        IOptions<EmailSettings> emailSettings,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        // 拼接前端验证页面的完整链接
        // Uri.EscapeDataString：Token 是 Base64 编码，可能含有 +、/、= 等
        // 在 URL 里有特殊含义的字符，必须转义，否则前端收到的 token 会损坏
        var verificationLink =
            $"{_emailSettings.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        var message = new EmailMessage
        {
            From = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>",
            To = email,
            Subject = "验证你的 UUcars 邮箱",
            HtmlBody = BuildVerificationEmailHtml(verificationLink)
        };


        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    public async Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        var resetLink =
            $"{_emailSettings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var message = new EmailMessage();
        message.From = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>";
        message.To.Add(email);
        message.Subject = "重置你的 UUcars 密码";
        message.HtmlBody = BuildPasswordResetEmailHtml(resetLink);

        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    // =============================================
    // 邮件 HTML 模板
    // =============================================

    private static string BuildVerificationEmailHtml(string verificationLink)
    {
        return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; max-width: 600px;
                             margin: 0 auto; padding: 20px;">
                    <h2 style="color: #333;">验证你的邮箱</h2>
                    <p>感谢注册 UUcars！请点击下方按钮完成邮箱验证：</p>
                    <a href="{verificationLink}"
                       style="display: inline-block; padding: 12px 24px;
                              background-color: #2563eb; color: white;
                              text-decoration: none; border-radius: 6px; margin: 16px 0;">
                        验证邮箱
                    </a>
                    <p style="color: #666; font-size: 14px;">
                        链接有效期为 24 小时。如果你没有注册 UUcars，请忽略此邮件。
                    </p>
                    <p style="color: #999; font-size: 12px;">
                        如果按钮无法点击，请复制以下链接到浏览器：<br/>
                        <a href="{verificationLink}">{verificationLink}</a>
                    </p>
                </body>
                </html>
                """;
    }

    private static string BuildPasswordResetEmailHtml(string resetLink)
    {
        return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; max-width: 600px;
                             margin: 0 auto; padding: 20px;">
                    <h2 style="color: #333;">重置你的密码</h2>
                    <p>我们收到了你的密码重置请求，请点击下方按钮设置新密码：</p>
                    <a href="{resetLink}"
                       style="display: inline-block; padding: 12px 24px;
                              background-color: #2563eb; color: white;
                              text-decoration: none; border-radius: 6px; margin: 16px 0;">
                        重置密码
                    </a>
                    <p style="color: #666; font-size: 14px;">
                        链接有效期为 1 小时。如果你没有发起此请求，
                        请忽略此邮件，你的密码不会被修改。
                    </p>
                    <p style="color: #999; font-size: 12px;">
                        如果按钮无法点击，请复制以下链接到浏览器：<br/>
                        <a href="{resetLink}">{resetLink}</a>
                    </p>
                </body>
                </html>
                """;
    }
}