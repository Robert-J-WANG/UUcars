using Amazon.Runtime;
using Amazon.S3;
using UUcars.API.Configurations;

namespace UUcars.API.Extensions;

public static class StorageExtensions
{
    public static IServiceCollection AddStorageService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageSettings>(
            configuration.GetSection("StorageSettings"));

        // 读取配置，启动时就验证是否存在
        var settings = configuration.GetSection("StorageSettings").Get<StorageSettings>()
                       ?? throw new InvalidOperationException(
                           "StorageSettings is not configured.");

        // 初始化 S3 客户端，指向 R2 的 endpoint
        var credentials = new BasicAWSCredentials(
            settings.AccessKeyId,
            settings.SecretAccessKey);

        var s3Client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            // R2 的 S3 兼容 endpoint 格式
            ServiceURL = $"https://{settings.AccountId}.r2.cloudflarestorage.com"
        });

        // 注册 IAmazonS3（Singleton：S3 客户端是线程安全的，可以全局共享）
        services.AddSingleton<IAmazonS3>(s3Client);

        return services;
    }
}