using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    // 密码最少 8 位，包含大小写字母和数字
    // 正则说明：
    //   (?=.*[a-z])  至少一个小写字母
    //   (?=.*[A-Z])  至少一个大写字母
    //   (?=.*\d)     至少一个数字
    //   .{8,}        最少 8 个字符
    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain uppercase, lowercase, and a number.")]
    public string Password { get; set; } = string.Empty;
}