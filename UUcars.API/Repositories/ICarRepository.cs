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
    
    // 新增：查询车辆详情（同时加载 Seller 和 Images）
// 为什么不直接修改 GetByIdAsync？
// GetByIdAsync 目前被状态变更操作（提交审核、修改、删除）使用，
// 这些操作不需要加载图片列表，加了反而是多余的查询。
// 用途不同的查询用不同的方法，各自只加载需要的数据
    Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);
}