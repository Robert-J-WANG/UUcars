using Google.Apis.Auth;
using UUcars.API.Exceptions;
using UUcars.API.Services.ExternalLogin;

namespace UUcars.Tests.Fakes;

public class FakeGoogleIdTokenService : IGoogleIdTokenService
{
    // 单元测试可传入不同 Payload；集成测试则使用默认 Google 用户。
    private readonly GoogleJsonWebSignature.Payload _payload;

    public FakeGoogleIdTokenService(
        GoogleJsonWebSignature.Payload? payload = null)
    {
        _payload = payload ?? new GoogleJsonWebSignature.Payload
        {
            Subject = "fake-google-subject",
            Email = "google-user@gmail.com",
            EmailVerified = true,
            Name = "Google Test User"
        };
    }

    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        // 用固定字符串模拟 Google 拒绝无效 ID Token 的结果。
        if (idToken == "invalid-google-token") throw new InvalidGoogleIdTokenException();

        // 测试关注 UUcars 如何处理已验证身份，不重复测试 Google SDK。
        return Task.FromResult(_payload);
    }
}
