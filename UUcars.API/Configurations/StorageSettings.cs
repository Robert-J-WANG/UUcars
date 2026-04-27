namespace UUcars.API.Configurations;

public class StorageSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    // R2 的 S3 兼容 endpoint，格式固定
    // 这个值由 AccountId 决定，在 StorageExtensions 里动态拼接
    // 不需要手动填写
    public string PublicUrl { get; set; } = string.Empty;
}