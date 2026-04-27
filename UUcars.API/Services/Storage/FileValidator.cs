namespace UUcars.API.Services.Storage;

public static class FileValidator
{
    // 最大文件大小：5MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // 允许的图片类型
    private static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    public static (bool IsValid, string? Error) Validate(IFormFile file)
    {
        if (file.Length == 0)
            return (false, "File is empty.");

        if (file.Length > MaxFileSizeBytes)
            return (false, "File size must not exceed 5MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return (false, "Only JPEG, PNG, and WebP images are allowed.");

        return (true, null);
    }

    // 根据原始文件名生成唯一的存储文件名
    // 格式：cars/{guid}.{扩展名}
    // 用 GUID 避免文件名冲突，用 cars/ 前缀在 bucket 里组织文件
    public static string GenerateFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        return $"cars/{Guid.NewGuid()}{extension}";
    }
}