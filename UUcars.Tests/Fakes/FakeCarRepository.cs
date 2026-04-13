using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeCarRepository : ICarRepository
{
    private readonly Dictionary<int, Car> _store = new();
    private int _nextId = 1;

    public void Seed(Car car)
    {
        if (car.Id == 0) car.Id = _nextId++;
        _store[car.Id] = car;
    }


    public Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default)
    {
        car.Id = _nextId++;
        _store[car.Id] = car;
        return Task.FromResult(car);
    }

    public Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var car);
        return Task.FromResult(car);
    }

    public Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        _store[car.Id] = car;
        return Task.FromResult(car);
    }

    // 新增：查询指定状态的车辆列表（支持分页 + 过滤）
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    public Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var cars = _store.Values.Where(c => c.Status == status).ToList();
        return Task.FromResult((cars, cars.Count));
    }


    //卖家列表页更新
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    public Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var cars = _store.Values.Where(c => c.SellerId == sellerId).ToList();
        return Task.FromResult((cars, cars.Count));
    }

    // 新增：查询车辆详情（同时加载 Seller 和 Images）
// 为什么不直接修改 GetByIdAsync？
// GetByIdAsync 目前被状态变更操作（提交审核、修改、删除）使用，
// 这些操作不需要加载图片列表，加了反而是多余的查询。
// 用途不同的查询用不同的方法，各自只加载需要的数据
    public Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var car);
        return Task.FromResult(car);
    }
}