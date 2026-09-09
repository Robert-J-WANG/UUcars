using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using UUcars.API.Auth;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services.ExternalLogin;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class ExternalLoginServiceTests
{
    // 每个测试传入自己的已验证 Payload，隔离真实 Google 网络请求。
    private static ExternalLoginService CreateService(
        GoogleJsonWebSignature.Payload payload,
        FakeUserRepository users,
        FakeExternalLoginRepository externalLogins)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-characters!",
            ExpiresInMinutes = 60,
            Issuer = "UUcars.Tests",
            Audience = "UUcars.Tests"
        });

        return new ExternalLoginService(
            new FakeGoogleIdTokenService(payload),
            externalLogins,
            users,
            new JwtTokenGenerator(jwtSettings));
    }

    // 验证：同一 Google Subject 再次登录时，必须回到原先关联的 UUcars User。
    [Fact]
    public async Task LoginAsync_WithKnownGoogleSubject_ShouldUseLinkedUser()
    {
        // 已经有 Provider + Subject 关联时，应直接使用关联的 User。
        var users = new FakeUserRepository();
        var externalLogins = new FakeExternalLoginRepository();

        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = "alice@gmail.com",
            PasswordHash = "existing-hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        externalLogins.Seed(new ExternalLogin
        {
            Id = 1,
            Provider = "Google",
            ProviderSubject = "known-google-subject",
            UserId = user.Id,
            User = user,
            CreatedAt = DateTime.UtcNow
        });

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "known-google-subject"
        };

        var service = CreateService(
            payload,
            users,
            externalLogins);

        var result = await service.LoginAsync(
            "valid-google-token",
            CancellationToken.None);

        Assert.Equal(user.Id, result.User.Id);
        Assert.NotEmpty(result.Token);
        Assert.Single(externalLogins.Items);
    }

    // 验证：已验证的本地账号可按同一 Gmail 地址新增 Google 登录关联。
    [Fact]
    public async Task LoginAsync_WithConfirmedLocalUser_ShouldCreateGoogleLink()
    {
        // 已验证的本地同邮箱账号可以自动增加 Google 登录方式。
        var users = new FakeUserRepository();
        var externalLogins = new FakeExternalLoginRepository();

        var user = new User
        {
            Id = 1,
            Username = "alice",
            Email = "alice@gmail.com",
            PasswordHash = "existing-hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        users.Seed(user);

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "new-google-subject",
            Email = "alice@gmail.com",
            EmailVerified = true,
            Name = "Alice"
        };

        var service = CreateService(
            payload,
            users,
            externalLogins);

        var result = await service.LoginAsync(
            "valid-google-token",
            CancellationToken.None);

        var externalLogin = Assert.Single(externalLogins.Items);

        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Id, externalLogin.UserId);
        Assert.Equal("Google", externalLogin.Provider);
        Assert.Equal("new-google-subject", externalLogin.ProviderSubject);
    }

    // 验证：首次 Google 登录会创建无 PasswordHash 的 User 及其关联记录。
    [Fact]
    public async Task LoginAsync_WithNewGoogleUser_ShouldCreateUserAndLink()
    {
        // 首次 Google 登录且没有同邮箱账号时，同时创建 User 和关联记录。
        var users = new FakeUserRepository();
        var externalLogins = new FakeExternalLoginRepository();

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "new-google-subject",
            Email = "new-user@gmail.com",
            EmailVerified = true,
            Name = "New Google User"
        };

        var service = CreateService(
            payload,
            users,
            externalLogins);

        var result = await service.LoginAsync(
            "valid-google-token",
            CancellationToken.None);

        var user = await users.GetByEmailAsync(
            "new-user@gmail.com");

        Assert.NotNull(user);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Null(user.PasswordHash);
        Assert.True(user.EmailConfirmed);

        var externalLogin = Assert.Single(user.ExternalLogins);

        Assert.Equal("Google", externalLogin.Provider);
        Assert.Equal("new-google-subject", externalLogin.ProviderSubject);
    }

    // 验证：未验证的本地同邮箱账号不能被 Google 登录自动关联。
    [Fact]
    public async Task LoginAsync_WithUnconfirmedLocalUser_ShouldThrowConflict()
    {
        // 未验证的本地账号不能仅凭同邮箱自动关联 Google 身份。
        var users = new FakeUserRepository();
        var externalLogins = new FakeExternalLoginRepository();

        users.Seed(new User
        {
            Id = 1,
            Username = "alice",
            Email = "alice@gmail.com",
            PasswordHash = "existing-hash",
            Role = UserRole.User,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "new-google-subject",
            Email = "alice@gmail.com",
            EmailVerified = true,
            Name = "Alice"
        };

        var service = CreateService(
            payload,
            users,
            externalLogins);

        await Assert.ThrowsAsync<ExternalLoginConflictException>(() => service.LoginAsync(
            "valid-google-token",
            CancellationToken.None));

        Assert.Empty(externalLogins.Items);
    }
}
