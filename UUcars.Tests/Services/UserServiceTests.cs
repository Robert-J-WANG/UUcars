using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UUcars.API.Auth;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class UserServiceTests
{
    // v3 更新：
    // - 返回 (service, fakeBackgroundJobClient) 元组
    // - fakeEmailService 内部保留但不再从外部断言邮件内容
    //   原因：邮件改为 Hangfire 异步入队，不再直接发送
    //   Token 的验证改为从 FakeUserRepository 直接读取
    private static (UserService Service, FakeBackgroundJobClient JobClient)
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
        var fakeBackgroundJobClient = new FakeBackgroundJobClient();

        var service = new UserService(
            repo,
            new PasswordHasher<User>(),
            new JwtTokenGenerator(jwtSettings),
            fakeEmailService,
            fakeBackgroundJobClient,
            NullLogger<UserService>.Instance
        );

        return (service, fakeBackgroundJobClient);
    }

    // ===== 注册测试 =====

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponseAndEnqueueEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        var result = await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);

        // V3 更新：验证邮件 Job 已入队（不再验证邮件是否直接发送）
        Assert.Single(jobClient.EnqueuedJobs);
        Assert.Contains("SendEmailVerificationAsync", jobClient.EnqueuedJobs);
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

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.RegisterAsync(
            "newuser",
            "existing@example.com",
            "Test@123456"
        ));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > 20);
    }

    // ===== 登录测试 =====

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowEmailNotConfirmedException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 注册后不验证邮箱，直接登录应被拒绝
        await Assert.ThrowsAsync<EmailNotConfirmedException>(() => service.LoginAsync(
            "test@example.com",
            "Test@123456"
        ));
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmail_ShouldReturnLoginResponse()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // V3 更新：Token 直接从 repo 读取，不再从 FakeEmailService 取
        // 原因：邮件改为异步入队，FakeEmailService 不再收到真实调用
        var user = await repo.GetByEmailAsync("test@example.com");
        var token = user!.EmailConfirmationToken;
        await service.VerifyEmailAsync(token!);

        var result = await service.LoginAsync(
            "test@example.com",
            "Test@123456"
        );

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "test@example.com",
            "WrongPassword"
        ));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "notexist@example.com",
            "Test@123456"
        ));
    }

    // ===== 邮箱验证测试 =====

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldConfirmEmail()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // V3 更新：直接从 repo 取 Token
        var user = await repo.GetByEmailAsync("test@example.com");
        var token = user!.EmailConfirmationToken;

        await service.VerifyEmailAsync(token!);

        var updated = await repo.GetByEmailAsync("test@example.com");
        Assert.True(updated!.EmailConfirmed);
        Assert.Null(updated.EmailConfirmationToken);
        Assert.Null(updated.EmailConfirmationTokenExpiry);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync(
            "completely-wrong-token"
        ));
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

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync(
            "expired-token"
        ));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithUnverifiedEmail_ShouldEnqueueEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 注册时已入队一次
        Assert.Single(jobClient.EnqueuedJobs);

        await service.ResendVerificationAsync("test@example.com");

        // 重发后共入队两次
        Assert.Equal(2, jobClient.EnqueuedJobs.Count);
        Assert.All(jobClient.EnqueuedJobs,
            job => Assert.Equal("SendEmailVerificationAsync", job));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithNonExistentEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 不存在的邮箱：静默返回，不入队
        await service.ResendVerificationAsync("nobody@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    // ===== 密码重置测试 =====

    [Fact]
    public async Task ForgotPasswordAsync_WithValidVerifiedEmail_ShouldEnqueueResetEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 注册 + 验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );
        var user = await repo.GetByEmailAsync("test@example.com");
        await service.VerifyEmailAsync(user!.EmailConfirmationToken!);

        // 发起重置密码
        await service.ForgotPasswordAsync("test@example.com");

        // 注册时入队一次(SendEmailVerificationAsync)
        // ForgotPassword 入队一次(SendPasswordResetAsync)
        Assert.Equal(2, jobClient.EnqueuedJobs.Count);
        Assert.Contains("SendPasswordResetAsync", jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        await service.ForgotPasswordAsync("nobody@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnverifiedEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 注册但不验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 清空注册时产生的入队记录，专注测试 ForgotPassword 的行为
        jobClient.EnqueuedJobs.Clear();

        // 未验证的账号不能重置密码，不应入队
        await service.ForgotPasswordAsync("test@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldUpdatePasswordAndClearToken()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        // 注册 + 验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );
        var user = await repo.GetByEmailAsync("test@example.com");
        await service.VerifyEmailAsync(user!.EmailConfirmationToken!);

        // 发起重置，Token 直接从 repo 读
        await service.ForgotPasswordAsync("test@example.com");
        var updatedUser = await repo.GetByEmailAsync("test@example.com");
        var resetToken = updatedUser!.ResetPasswordToken;

        // 用新密码重置
        await service.ResetPasswordAsync(resetToken!, "NewPassword@123");

        // 验证：新密码可以登录
        var loginResult = await service.LoginAsync(
            "test@example.com",
            "NewPassword@123"
        );
        Assert.NotEmpty(loginResult.Token);

        // 验证：旧密码不能再登录
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "test@example.com",
            "Test@123456"
        ));

        // 验证：Token 已清除，不能重复使用
        var finalUser = await repo.GetByEmailAsync("test@example.com");
        Assert.Null(finalUser!.ResetPasswordToken);
        Assert.Null(finalUser.ResetPasswordTokenExpiry);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(
            "invalid-token",
            "NewPassword@123"
        ));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            ResetPasswordToken = "expired-reset-token",
            ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(-1)
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(
            "expired-reset-token",
            "NewPassword@123"
        ));
    }
}