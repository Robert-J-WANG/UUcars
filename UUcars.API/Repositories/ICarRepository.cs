using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);

    // 新增：查询指定状态的车辆列表（支持分页 + 过滤）
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        CarQueryRequest query,
        CancellationToken cancellationToken = default);


    //卖家列表页更新
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        CarQueryRequest query,
        CancellationToken cancellationToken = default);
}