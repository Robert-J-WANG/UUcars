using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeCarImageRepository : ICarImageRepository
{
    private readonly Dictionary<int, CarImage> _store = new();
    private int _nextId = 1;


    public void Seed(CarImage image)
    {
        if (image.Id == 0) image.Id = _nextId++;
        _store[image.Id] = image;
    }

    public Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        image.Id = _nextId++;
        _store[image.Id] = image;
        return Task.FromResult(image);
    }

    public Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(imageId, out var image);
        return Task.FromResult(image);
    }

    public Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        _store.Remove(image.Id);
        return Task.CompletedTask;
    }

    // ✅ 新增
    public Task<List<CarImage>> AddRangeAsync(
        List<CarImage> images, CancellationToken cancellationToken = default)
    {
        foreach (var image in images)
        {
            image.Id = _nextId++;
            _store[image.Id] = image;
        }

        return Task.FromResult(images);
    }

    // ✅ 新增
    public Task<List<CarImage>> GetByCarIdAsync(
        int carId, CancellationToken cancellationToken = default)
    {
        var images = _store.Values.Where(i => i.CarId == carId).ToList();
        return Task.FromResult(images);
    }

    // ✅ 新增
    // _store 存的是对象引用，Service 层修改了 image.SortOrder 后这里已自动同步
    // 显式赋值一次是为了和真实 EfCarImageRepository 的调用约定保持一致
    public Task UpdateSortOrdersAsync(
        List<CarImage> images, CancellationToken cancellationToken = default)
    {
        foreach (var image in images)
            _store[image.Id] = image;
        return Task.CompletedTask;
    }
}