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
    
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange：先注册一个用户，确保数据库里有正确 Hash 过的密码
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // Act：用同样的密码登录
        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // Assert
        Assert.NotNull(result);
        // Token 不为空字符串，说明 JWT 生成成功
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("test@example.com", result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange：先注册用户
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // Act + Assert：用错误密码登录，必须抛出 InvalidCredentialsException
        // 注意：不是 "密码错误异常"，而是 InvalidCredentialsException——
        // 邮箱不存在和密码错误返回同一个异常，不透露具体原因
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            }));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        // Arrange：空 repo，没有任何用户
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        // Act + Assert：邮箱不存在时抛出的异常和密码错误完全一样
        // 这是有意为之的安全设计：攻击者无法通过错误信息判断邮箱是否存在
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest
            {
                Email = "notexist@example.com",
                Password = "Test@123456"
            }));
    }
    
}