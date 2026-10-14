namespace UUcars.API.Entities;

public class ExternalLogin
{
    public int Id { get; set; }

    // 当前使用 Google
    public string Provider { get; set; } = string.Empty;

    // Google ID Token 中的 sub
    public string ProviderSubject { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}