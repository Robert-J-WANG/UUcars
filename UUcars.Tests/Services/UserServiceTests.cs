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
    // 测试用的辅助方法：创建一个标准的 UserService 实例
    // NullLogger：空日志，测试里不关心日志输出
    // PasswordHasher<User>：用真实实现，它是纯计算，不依赖外部资源
    private static UserService CreateService(FakeUserRepository repo)
    {
        // 给测试用的 JwtSettings，值随便填——注册测试根本不会走到生成 Token 的逻辑
        // 但 UserService 构造函数需要这个依赖，所以必须传进去
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-characters!",
            ExpiresInMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        return new UserService(repo, new PasswordHasher<User>(), new JwtTokenGenerator(jwtSettings),NullLogger<UserService>.Instance);
    }
    
    
    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponse()
    {
        // Arrange：准备一个空的 Repository（没有任何用户）
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        };

        // Act：执行注册
        var result = await service.RegisterAsync(request);

        // Assert：验证返回的数据是否符合预期
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);   // 默认角色是 User
    }
    
    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange：预先插入一个用户，模拟"邮箱已被注册"
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = "somehash",
            Role = UserRole.User
        });

        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",  // 用同一个邮箱
            Password = "Test@123456"
        };

        // Act + Assert：必须抛出 UserAlreadyExistsException
        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        // Arrange
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        };

        // Act
        await service.RegisterAsync(request);

        // Assert：数据库里存的不应该是明文密码
        // 通过 Fake Repo 直接读取"数据库"里的记录
        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);  // 不等于明文
        Assert.True(stored.PasswordHash.Length > 20);          // Hash 字符串很长
    }
    
}