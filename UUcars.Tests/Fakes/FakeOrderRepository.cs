using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, Order> _store = new();
    private int _nextId = 1;

    public Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        order.Id = _nextId++;
        _store[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(o => o.BuyerId == buyerId)
            .ToList();
        return Task.FromResult((result, result.Count));
    }

    public Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
        int sellerId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(o => o.SellerId == sellerId)
            .ToList();
        return Task.FromResult((result, result.Count));
    }

    public void Seed(Order order)
    {
        if (order.Id == 0) order.Id = _nextId++;
        _store[order.Id] = order;
    }
}