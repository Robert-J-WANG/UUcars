using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UUcars.API.Auth;
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class UserServiceTests
{
    // 返回 (service, fakeEmailService) 元组
    // fakeEmailService 供测试里断言邮件是否发送、发给谁、Token 是什么
    private static (UserService Service, FakeEmailService EmailService)
        CreateService(FakeUserRepository repo)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-characters!",
            ExpiresInMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });

        var fakeEmailService = new FakeEmailService();

        var service = new UserService(
            repo,
            new PasswordHasher<User>(),
            new JwtTokenGenerator(jwtSettings),
            fakeEmailService,
            NullLogger<UserService>.Instance
        );

        return (service, fakeEmailService);
    }

    // ===== 注册测试（更新）=====

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponseAndSendEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);

        // V2 新增：验证邮件已发送
        Assert.Single(emailService.SentVerificationEmails);
        Assert.Equal("test@example.com",
            emailService.SentVerificationEmails[0].Email);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserAlreadyExistsException()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = "somehash",
            Role = UserRole.User
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.RegisterAsync(new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Test@123456"
        }));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > 20);
    }

    // ===== 登录测试（更新）=====

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowEmailNotConfirmedException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        // 注册但不验证邮箱
        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 直接尝试登录，应该被拒绝（密码正确但邮箱未验证）
        await Assert.ThrowsAsync<EmailNotConfirmedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123456"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmail_ShouldReturnLoginResponse()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 用注册时发出的 Token 完成邮箱验证
        var token = emailService.SentVerificationEmails[0].Token;
        await service.VerifyEmailAsync(token);

        // 验证后登录应该成功
        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "notexist@example.com",
            Password = "Test@123456"
        }));
    }

    // ===== 邮箱验证测试（V2 新增）=====

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldConfirmEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 从 FakeEmailService 里取出注册时发出的 Token
        var token = emailService.SentVerificationEmails[0].Token;

        await service.VerifyEmailAsync(token);

        var user = await repo.GetByEmailAsync("test@example.com");
        Assert.True(user!.EmailConfirmed);
        Assert.Null(user.EmailConfirmationToken); // Token 已清除
        Assert.Null(user.EmailConfirmationTokenExpiry);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync("completely-wrong-token"));
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();

        repo.Seed(new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Role = UserRole.User,
            EmailConfirmed = false,
            EmailConfirmationToken = "expired-token",
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(-1)
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync("expired-token"));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithUnverifiedEmail_ShouldSendNewEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.Single(emailService.SentVerificationEmails);

        await service.ResendVerificationAsync("test@example.com");

        // 重发后共有两封验证邮件
        Assert.Equal(2, emailService.SentVerificationEmails.Count);
    }

    [Fact]
    public async Task ResendVerificationAsync_WithNonExistentEmail_ShouldNotThrowOrSendEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        // 不存在的邮箱：静默返回，不发邮件，不抛异常
        await service.ResendVerificationAsync("nobody@example.com");

        Assert.Empty(emailService.SentVerificationEmails);
    }
}