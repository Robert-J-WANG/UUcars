using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class AdminCarServiceTests
{
    [Fact]
    public async Task ApproveAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
    {
        // Arrange
        var fakeRepo = new ConcurrencyCarRepository();
        var service = new AdminCarService(
            fakeRepo,
            NullLogger<AdminCarService>.Instance,
            new FakeCacheService()
        );

        fakeRepo.Seed(new Car
        {
            Id = 1,
            Title = "Test Car",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Price = 10000,
            Mileage = 50000,
            SellerId = 1,
            Status = CarStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.ApproveAsync(1));
    }

    [Fact]
    public async Task RejectAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
    {
        // Arrange
        var fakeRepo = new ConcurrencyCarRepository();
        var service = new AdminCarService(
            fakeRepo,
            NullLogger<AdminCarService>.Instance,
            new FakeCacheService()
        );

        fakeRepo.Seed(new Car
        {
            Id = 1,
            Title = "Test Car",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Price = 10000,
            Mileage = 50000,
            SellerId = 1,
            Status = CarStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.RejectAsync(1));
    }
}

/// <summary>
///     专门用于测试并发冲突的假 Repository
///     UpdateAsync 固定抛 DbUpdateConcurrencyException，模拟 RowVersion 不匹配的场景
/// </summary>
/// 
/// <remarks>
///     为什么用显式接口实现而不是 override 或 new？
///
///     FakeCarRepository.UpdateAsync 不是 virtual，无法 override。
///
///     new（方法隐藏）虽然可以声明同名方法，但通过接口调用时走的仍是父类的实现，
///     因为 new 只影响继承链，不影响接口的方法映射。
///
///     显式接口实现（ICarRepository.UpdateAsync(...)）会直接绑定到
///     接口的方法映射上，优先级高于继承来的实现，所以通过接口调用时会走这里。
/// 
///     其他未显式实现的方法仍继承自 FakeCarRepository。
/// </remarks>
internal class ConcurrencyCarRepository : FakeCarRepository, ICarRepository
{
    Task<Car> ICarRepository.UpdateAsync(
        Car car,
        CancellationToken cancellationToken)
    {
        // 模拟乐观锁检测到 RowVersion 不匹配时抛出的异常
        throw new DbUpdateConcurrencyException(
            "Simulated concurrency conflict: RowVersion mismatch");
    }
}