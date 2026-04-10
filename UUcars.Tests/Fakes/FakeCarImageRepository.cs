using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeCarImageRepository:ICarImageRepository
{
   private readonly Dictionary<int, CarImage> _store = new();
   private int _nextId = 1;
   
   // public void Seed(CarImage image)
   // {
   //    if (image.Id == 0) image.Id = _nextId++;
   //    _store[image.Id] = image;
   // }

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
}