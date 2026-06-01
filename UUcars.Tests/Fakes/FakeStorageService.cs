using UUcars.API.Services.Storage;

namespace UUcars.Tests.Fakes;

public class FakeStorageService : IStorageService
{
    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        // 返回一个假 URL，格式和 R2StorageService 保持一致
        return Task.FromResult($"https://fake-storage.example.com/{fileName}");
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}