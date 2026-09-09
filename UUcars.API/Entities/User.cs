using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Google 首次登录创建的用户没有 UUCars 本地密码
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailConfirmed { get; set; } = false;

    // Step 38：邮箱验证
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // Step 39：密码重置
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpiry { get; set; }

    // 导航属性
    // 一个用户可以发布多辆车
    public ICollection<Car> Cars { get; set; } = [];

    // 一个用户可以有多个"买家身份"的订单
    public ICollection<Order> BuyerOrders { get; set; } = [];

    // 一个用户可以有多个"卖家身份"的订单
    public ICollection<Order> SellerOrders { get; set; } = [];

    // 一个用户可以收藏多辆车
    public ICollection<Favorite> Favorites { get; set; } = [];

    // 一个用户可以有多个refresh token
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // 本地用户可以没有关联记录；第三方登录用户保存对应的关联记录
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
}
