using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UUcars.API.Configurations;

namespace UUcars.API.Services.Storage;

public class R2StorageService : IStorageService
{
    private readonly ILogger<R2StorageService> _logger;
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _storageSettings;

    public R2StorageService(
        IAmazonS3 s3Client,
        IOptions<StorageSettings> storageSettings,
        ILogger<R2StorageService> logger)
    {
        _s3Client = s3Client;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _storageSettings.BucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType,
            // R2 必须设置这两个标志
            // 原因：R2 不支持 AWS SDK 默认的 Streaming SigV4 签名方式
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        // 拼接公开访问 URL
        var publicUrl = $"{_storageSettings.PublicUrl.TrimEnd('/')}/{fileName}";

        _logger.LogInformation("File uploaded to R2: {Url}", publicUrl);

        return publicUrl;
    }

    // 删除文件
    // fileName：存储时使用的文件名
    public async Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(
            _storageSettings.BucketName, fileName, cancellationToken);

        _logger.LogInformation("File deleted from R2: {FileName}", fileName);
    }
}