using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    // 买家视角：查询某用户作为买家的订单列表
    Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // 卖家视角：查询某用户作为卖家的订单列表
    Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}