using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google ID token is required.")]
    public string IdToken { get; set; } = string.Empty;
}