using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarRepository
{ 
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
}