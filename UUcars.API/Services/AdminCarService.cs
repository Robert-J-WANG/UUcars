using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services.Cache;

namespace UUcars.API.Services;

public class AdminCarService
{
    private readonly ICacheService _cache; // ✅ 新增
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;


    public AdminCarService(ICarRepository carRepository, ILogger<AdminCarService> logger, ICacheService cache)
    {
        _carRepository = carRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<CarResponse> ApproveAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只有 PendingReview 状态才能审核通过
        // 已经 Published 的车不需要重复审核
        // Draft 的车还没提交，不应该直接被 Admin 通过
        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 车辆上架 → 公开列表变了 → 清掉公开列表的所有缓存
        // 用前缀删除：一次清掉所有分页（page1/page2/page3...）
        await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation("Car {CarId} approved by admin, now Published", carId);

        return CarService.MapToResponse(updated);
    }

    public async Task<CarResponse> RejectAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 同样只有 PendingReview 状态才能被拒绝
        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        // 拒绝后退回 Draft，让卖家修改后重新提交
        car.Status = CarStatus.Draft;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 车辆退回 → 待审核列表变了 → 清待审核列表缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation("Car {CarId} rejected by admin, returned to Draft", carId);

        return CarService.MapToResponse(updated);
    }


    // ✅ 待审核车辆列表：加缓存
    public async Task<PagedResponse<CarResponse>> GetPendingCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        // 构建缓存 Key（包含所有过滤参数，不同参数对应不同缓存）
        var cacheKey = CacheKeys.PendingCars(page, pageSize);

        // GetOrSetAsync：有缓存直接返回；没有则查数据库并缓存结果

        return await _cache.GetOrSetAsync(cacheKey,
            async () =>
            {
                var (cars, totalCount) = await _carRepository.GetPagedAsync(
                    CarStatus.PendingReview,
                    query, cancellationToken);
                var items = cars.Select(CarService.MapToResponse).ToList();
                return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
            }, TimeSpan.FromSeconds(30), cancellationToken);
    }

    public async Task AdminDeleteAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // Admin 可以删除任何状态的车辆，不受状态约束
        // 但已经是 Deleted 状态的车不需要重复操作
        if (car.Status == CarStatus.Deleted)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Published);

        var previousStatus = car.Status; // ✅ 记录删除前的状态
        car.Status = CarStatus.Deleted;
        car.UpdatedAt = DateTime.UtcNow;

        await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 根据删除前的状态，清对应的缓存
        if (previousStatus == CarStatus.Published)
            await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);

        if (previousStatus == CarStatus.PendingReview)
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation(
            "Car {CarId} forcefully deleted by admin (was {Status}), cache invalidated",
            carId, previousStatus);
    }
}