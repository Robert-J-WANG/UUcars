namespace UUcars.API.Configurations;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // 前端地址，用于拼接邮件里的跳转链接
    // 开发环境：http://localhost:5173
    // 生产环境：https://yourdomain.com
    public string BaseUrl { get; set; } = string.Empty;
}