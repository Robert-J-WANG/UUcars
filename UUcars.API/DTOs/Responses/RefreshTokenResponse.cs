namespace UUcars.API.DTOs.Responses;

public class RefreshTokenResponse
{
    /// <summary>
    /// 新签发的 AccessToken
    /// 前端收到后更新 authStore，替换旧的 Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
}