using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}