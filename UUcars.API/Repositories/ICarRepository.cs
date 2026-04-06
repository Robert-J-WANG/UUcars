using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarRepository
{ 
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
    
    // 新增
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);
}