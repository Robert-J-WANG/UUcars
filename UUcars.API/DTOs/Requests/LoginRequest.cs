using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public String Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    public String Password { get; set; } = string.Empty;
}