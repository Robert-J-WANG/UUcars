using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class AdminCarService
{
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;

    public AdminCarService(ICarRepository carRepository, ILogger<AdminCarService> logger)
    {
        _carRepository = carRepository;
        _logger = logger;
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

        _logger.LogInformation("Car {CarId} rejected by admin, returned to Draft", carId);

        return CarService.MapToResponse(updated);
    }

    public async Task<PagedResponse<CarResponse>> GetPendingCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        // 直接复用 ICarRepository 的 GetPagedAsync，传入 PendingReview 状态
        // 不需要新增 Repository 方法，这正是把 status 作为参数设计的价值
        var (cars, totalCount) = await _carRepository.GetPagedAsync(
            CarStatus.PendingReview,
            query,
            cancellationToken);

        var items = cars.Select(CarService.MapToResponse).ToList();

        return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
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

        car.Status = CarStatus.Deleted;
        car.UpdatedAt = DateTime.UtcNow;

        await _carRepository.UpdateAsync(car, cancellationToken);

        _logger.LogInformation("Car {CarId} forcefully deleted by admin", carId);
    }
}