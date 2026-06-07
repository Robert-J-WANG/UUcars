namespace UUcars.API.Extensions;

/// <summary>
///     限流策略名称常量
///     用于 [EnableRateLimiting("xxx")] 和策略注册时保持一致
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Register = "register";
    public const string ForgotPassword = "forgot-password";
    public const string ResendVerification = "resend-verification";
    public const string Browse = "browse";
    public const string Write = "write";
}