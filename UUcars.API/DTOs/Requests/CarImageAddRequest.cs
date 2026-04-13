using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarImageAddRequest
{
    // V1 只支持提交 URL，不做真实文件上传
    // URL 格式验证：[Url] 特性会检查是否是合法的 HTTP/HTTPS 地址
    [Required(ErrorMessage = "ImageUrl is required.")]
    [Url(ErrorMessage = "ImageUrl must be a valid URL.")]
    [MaxLength(500, ErrorMessage = "ImageUrl must not exceed 500 characters.")]
    public string ImageUrl { get; set; } = string.Empty;

    // 显示顺序，默认 0（第一张图片作为封面）
    // 客户端可以传入顺序值控制图片排列
    public int SortOrder { get; set; } = 0;
}