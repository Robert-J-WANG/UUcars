using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarImageRepository
{
    Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default);
    Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default);
    Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default);
}