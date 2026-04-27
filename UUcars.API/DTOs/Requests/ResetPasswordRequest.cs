using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;


    [Required(ErrorMessage = "New password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain " +
                       "uppercase, lowercase, and a number.")]
    public string NewPassword { get; set; } = string.Empty;
}