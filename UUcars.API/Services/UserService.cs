using Microsoft.AspNetCore.Identity;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;  // 改为接口
    private readonly ILogger<UserService> _logger;

    // 通过构造函数注入，由 DI 容器提供
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,   // 改为接口注入
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 检查邮箱是否已被注册
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new UserAlreadyExistsException(request.Email);
        }

        // 2. 构建用户实体（先不填 PasswordHash）
        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLower(), // 统一存小写，保持一致性
            Role = UserRole.User, // 注册的用户默认是普通用户
            EmailConfirmed = false, // V1 不做邮箱验证，默认 false
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Hash 密码（细节在 Step 09 讲解）
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        // 4. 存入数据库
        var created = await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("New user registered: {Email}", created.Email);

        // 5. 返回 DTO（不包含 PasswordHash）
        return MapToResponse(created);
    }

    // 实体 → DTO 的映射方法
    // 写成 private static：不依赖实例状态，也不需要从外部调用
    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}