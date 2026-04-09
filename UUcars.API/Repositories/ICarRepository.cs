using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);


    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);

    // 新增：查询指定状态的车辆列表（支持分页）
    Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // 新增：查询某个卖家的所有车辆（不过滤状态，支持分页）
    // 返回 (数据列表, 总数量) 的元组
    // 为什么同时返回 totalCount？
    // 分页接口需要告诉客户端一共有多少条数据才能渲染分页控件，
    // 如果分两次查询（一次取数据、一次 COUNT），会有两次数据库往返。
    // 用元组一起返回，在 Repository 里一次性处理两个查询
    Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page, 
        int pageSize,
        CancellationToken cancellationToken = default);
}