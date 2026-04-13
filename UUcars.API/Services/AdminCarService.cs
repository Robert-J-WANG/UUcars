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
}