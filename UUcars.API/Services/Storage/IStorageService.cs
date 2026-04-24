namespace UUcars.API.Services.Storage;

public interface IStorageService
{
    // 上传文件，返回可公开访问的 URL
    // fileName：存储时使用的文件名（调用方负责生成唯一文件名）
    // contentType：文件 MIME 类型，比如 image/jpeg
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    // 删除文件
    // fileName：存储时使用的文件名
    Task DeleteAsync(string fileName,
        CancellationToken cancellationToken = default);
}