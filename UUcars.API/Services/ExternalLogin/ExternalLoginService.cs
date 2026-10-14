using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth;
using UUcars.API.Auth;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services.ExternalLogin;

public class ExternalLoginService
{
    private const string GoogleProvider = "Google";
    private readonly IGoogleIdTokenService _googleIdTokenService;
    private readonly IExternalLoginRepository _externalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly JwtTokenGenerator _jwtTokenGenerator;


    public ExternalLoginService(IGoogleIdTokenService googleIdTokenService,
        IExternalLoginRepository externalLoginRepository, IUserRepository userRepository,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _googleIdTokenService = googleIdTokenService;
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(string idToken, CancellationToken cancellationToken)
    {
        // 获取认证后的payload
        var payload = await _googleIdTokenService.ValidateAsync(idToken, cancellationToken);

        // 查询关联用户的逻辑
        var user = await FindOrCreateUserAsync(payload, cancellationToken);

        // 生成用户的Access Token
        var token = _jwtTokenGenerator.GenerateToken(user);
        // 读取 过期时间
        var expiresAt = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .ValidTo;

        // 返回登录结果LoginResponse
        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        };
    }

    private async Task<User> FindOrCreateUserAsync(GoogleJsonWebSignature.Payload payload,
        CancellationToken cancellationToken = default)
    {
        // 查询关联用户的逻辑
        // 1. 已经存在第三方关联——————————————————————————————

        var existingLogin = await _externalLoginRepository.GetByProviderAndSubjectAsync(GoogleProvider,
            payload.Subject,
            cancellationToken);

        if (existingLogin != null) return existingLogin.User;

        // 2. 没有关联用户——————————————————————————————
        // 处理邮件的逻辑
        if (string.IsNullOrWhiteSpace(payload.Email) ||
            !payload.EmailVerified)
            throw new ExternalLoginNotAllowedException(
                "A verified email address is required.");

        var email = payload.Email.Trim().ToLowerInvariant();
        if (email.Length > 100)
            throw new ExternalLoginNotAllowedException(
                "The email address exceeds the supported length.");
        var googleConfirmsEmailOwnership =
            email.EndsWith("@gmail.com", StringComparison.Ordinal) ||
            !string.IsNullOrWhiteSpace(payload.HostedDomain);

        if (!googleConfirmsEmailOwnership)
            throw new ExternalLoginNotAllowedException(
                "Please use email registration or your existing sign-in method.");

        // 此邮箱是否已经是本地注册用户
        var existingUser = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);
        // 是本地注册用户
        if (existingUser != null)
        {
            // 邮件为激活，抛冲突异常
            if (!existingUser.EmailConfirmed) throw new ExternalLoginConflictException();
            // 邮件已激活， 记录新的 externalLogin 数据
            var externalLogin = new Entities.ExternalLogin
            {
                Provider = GoogleProvider,
                ProviderSubject = payload.Subject,
                UserId = existingUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _externalLoginRepository.AddAsync(externalLogin, cancellationToken);

            return existingUser;
        }

        // 不是本地用户， 需要创建新用户，并同时记录 externalLogin 数据
        var username = string.IsNullOrWhiteSpace(payload.Name)
            ? email.Split('@')[0]
            : payload.Name.Trim();

        var user = new User
        {
            Username = username[..Math.Min(username.Length, 50)],
            Email = email,
            PasswordHash = null,
            Role = UserRole.User,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var newExternalLogin = new Entities.ExternalLogin
        {
            Provider = GoogleProvider,
            ProviderSubject = payload.Subject,
            CreatedAt = DateTime.UtcNow
        };

        return await _userRepository.AddWithExternalLoginAsync(user, newExternalLogin, cancellationToken);
    }
}