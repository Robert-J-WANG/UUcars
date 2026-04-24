using Microsoft.AspNetCore.Identity;
using UUcars.API.Auth;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services.Email;

namespace UUcars.API.Services;

public class UserService
{
    // email服务
    private readonly IEmailService _emailService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<UserService> _logger;
    private readonly IPasswordHasher<User> _passwordHasher; // 改为接口
    private readonly IUserRepository _userRepository;


    // 通过构造函数注入，由 DI 容器提供
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher, // 改为接口注入
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(string username, string email, string password,
        CancellationToken cancellationToken = default)
    {
        // 1. 检查邮箱是否已被注册
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing != null) throw new UserAlreadyExistsException(email);

        // =============================================
        // 注册（V2 更新：注册后发验证邮件）
        // =============================================


        // 2. 构建用户实体（先不填 PasswordHash）
        var user = new User
        {
            Username = username,
            Email = email.ToLower(), // 统一存小写，保持一致性
            Role = UserRole.User, // 注册的用户默认是普通用户
            EmailConfirmed = false, // V1 不做邮箱验证，默认 false
            // 邮件验证的token
            EmailConfirmationToken = TokenGenerator.Generate(),
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Hash 密码（细节在 Step 09 讲解）
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        // 4. 存入数据库
        var created = await _userRepository.AddAsync(user, cancellationToken);

        // =============================================
        // 注册（V2 更新：注册后发验证邮件）
        // =============================================

        // 先保存用户，再发邮件
        // 顺序很重要：确保用户已写入数据库，邮件里的验证链接才有意义
        // 如果反过来——邮件发出去了但数据库写入失败，
        // 用户点链接时服务端找不到这个 Token，验证永远失败
        // 如果邮件发送失败，用户可以通过"重新发送"功能补救
        await _emailService.SendEmailVerificationAsync(created.Email, created.EmailConfirmationToken!,
            cancellationToken);

        _logger.LogInformation("New user registered: {Email}", created.Email);

        // 5. 返回 DTO（不包含 PasswordHash）
        return MapToResponse(created);
    }

    public async Task<LoginResponse> LoginAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        // 1. 按邮箱查找用户
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // 2. 用户不存在：抛出和密码错误一样的异常，不透露"邮箱不存在"
        if (user == null) throw new InvalidCredentialsException();

        // 3. 验证密码
        // VerifyHashedPassword 做的事：
        //   从 user.PasswordHash 字符串里提取出当初 HashPassword 时用的 Salt
        //   用这个 Salt 对 request.Password（用户输入的明文）重新计算 Hash
        //   把计算结果和 user.PasswordHash 比对
        // 返回值是 PasswordVerificationResult 枚举：
        //   Success             → 匹配，验证通过
        //   Failed              → 不匹配，密码错误
        //   SuccessRehashNeeded → 匹配，但 Hash 算法版本较旧，建议更新
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) throw new InvalidCredentialsException();

        // =============================================
        // 登录（V2 更新：检查邮箱是否已验证）
        // =============================================

        // 邮箱未验证不允许登录
        // 密码正确但邮箱未验证 → 403，提示用户去验证邮箱
        // 前端收到 403 后可以显示"请先验证邮箱，或重新发送验证邮件"的提示
        if (!user.EmailConfirmed)
            throw new EmailNotConfirmedException();


        // 4. 验证通过，生成 JWT Token
        var token = _jwtTokenGenerator.GenerateToken(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponse
        {
            Token = token,
            // ExpiresAt 和 JwtTokenGenerator 里的 expires 保持一致
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToResponse(user)
        };
    }

    public async Task<UserResponse> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        return MapToResponse(user);
    }

    // =============================================
    // 验证邮箱（V2 新增）
    // =============================================
    public async Task VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // 用 Token 找到对应的用户
        // 找不到说明 Token 不存在（从未发过或已被清除）
        var user = await _userRepository
            .GetByEmailConfirmationTokenAsync(token, cancellationToken);

        if (user == null)
            throw new InvalidTokenException();

        if (user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            throw new InvalidTokenException();

        // 已经验证过（用户点了两次验证链接）
        if (user.EmailConfirmed)
            throw new EmailAlreadyConfirmedException();

        // 验证通过：标记已验证，清除 Token（一次性使用，用完即删）
        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Email verified for user: {Email}", user.Email);
    }

    // =============================================
    // 重新发送验证邮件（V2 新增）
    // =============================================
    public async Task ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(
            email, cancellationToken);

        // 安全设计：用户不存在时静默返回，不透露邮箱是否注册
        // 和 Step 39 忘记密码接口保持一致的安全原则：
        // 不能让攻击者通过这个接口枚举系统里注册过哪些邮箱
        if (user == null)
        {
            _logger.LogWarning(
                "Resend verification requested for non-existent email: {Email}",
                email);
            return;
        }

        if (user.EmailConfirmed)
            throw new EmailAlreadyConfirmedException();

        // 生成新 Token，旧 Token 同时作废
        // 每次重发都刷新 Token 和有效期，避免旧链接仍然有效
        var newToken = TokenGenerator.Generate();
        user.EmailConfirmationToken = newToken;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _emailService.SendEmailVerificationAsync(
            user.Email, newToken, cancellationToken);

        _logger.LogInformation("Verification email resent to: {Email}", email);
    }

    // =============================================
    // 忘记密码：发送重置邮件（Step 39 新增）
    // =============================================
    public async Task ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // 安全设计：用户不存在时静默返回，不透露邮箱是否注册
        // 和 ResendVerificationAsync 保持完全一致的安全原则：
        // 攻击者无法通过这个接口的响应差异，枚举系统里注册过哪些邮箱
        if (user == null)
        {
            _logger.LogWarning(
                "Password reset requested for non-existent email: {Email}", email);
            return;
        }

        // 未验证邮箱的账号不允许重置密码
        // 原因：如果允许未验证账号重置密码，攻击者可以用别人的邮箱注册，
        // 然后通过密码重置拿到 Token，等于变相控制了这个邮箱对应的账号
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning(
                "Password reset requested for unverified email: {Email}", email);
            return;
        }

        var resetToken = TokenGenerator.Generate();
        user.ResetPasswordToken = resetToken;
        // 有效期只有 1 小时，比邮箱验证（24小时）更短
        // 密码是更敏感的操作，万一邮件被截获，留给攻击者的时间窗口应该尽可能短
        user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _emailService.SendPasswordResetAsync(
            user.Email, resetToken, cancellationToken);

        _logger.LogInformation("Password reset email sent to: {Email}", email);
    }

    // =============================================
    // 重置密码：验证 Token + 更新密码（Step 39 新增）
    // =============================================
    public async Task ResetPasswordAsync(
        string token, string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByResetPasswordTokenAsync(
            token, cancellationToken);

        if (user == null)
            throw new InvalidTokenException();

        if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            throw new InvalidTokenException();

        // 用新密码覆盖旧密码
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

        // Token 立即销毁，防止同一个 Token 被重复使用
        // 即使邮件被截获，Token 用完一次就彻底失效
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation(
            "Password reset successfully for: {Email}", user.Email);
    }

    public async Task<UserResponse> UpdateCurrentUserAsync(
        int userId,
        string username,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        // 如果邮箱发生了变化，检查新邮箱是否已被其他人使用
        // 这里用 OrdinalIgnoreCase 做大小写不敏感的比较
        // 原因：用户输入的邮箱大小写可能和数据库里存的不一致（数据库存的是小写）
        // 不能直接用 == 比较，否则 "Test@example.com" 和 "test@example.com" 会被判断为不同
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);

            // existing.Id != userId：排除自己
            // 场景：用户只改了用户名，邮箱没变，或者邮箱大小写变了但本质一样
            // 如果不排除自己，用原邮箱提交也会触发"邮箱已被注册"的错误
            if (existing != null && existing.Id != userId)
                throw new UserAlreadyExistsException(email);
        }

        user.Username = username;
        user.Email = email.ToLower();
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User updated: {Email}", updated.Email);

        return MapToResponse(updated);
    }

    // 实体 → DTO 的映射方法
    // 写成 private static：不依赖实例状态，也不需要从外部调用
    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}