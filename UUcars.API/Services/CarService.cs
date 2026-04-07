using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class CarService
{
    private readonly ICarRepository _carRepository;
    private readonly ILogger<CarService> _logger;

    public CarService(ICarRepository carRepository, ILogger<CarService> logger)
    {
        _carRepository = carRepository;
        _logger = logger;
    }

    public async Task<CarResponse> CreateAsync(
        int sellerId,
        CarCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = new Car
        {
            Title = request.Title,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            Description = request.Description,
            SellerId = sellerId,            // 从 Token 里取到的当前用户 Id
            Status = CarStatus.Draft,       // 创建时强制为 Draft，客户端无法指定
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _carRepository.AddAsync(car, cancellationToken);
        // 再查一次，把 Seller 带出来
        

        _logger.LogInformation("Car created: {CarId} by seller {SellerId}", created.Id, sellerId);

        // Create 完之后，返回里必须要有 SellerUsername的话，Add 完 → 再查一次（因为GetByIdAsync带 Include， 手动加载car.Seller（导航属性）），这样car.Seller就不是null了
        var carWithSeller= await _carRepository.GetByIdAsync(created.Id, cancellationToken);
        return MapToResponse(carWithSeller!);
    }


    public async Task<CarResponse> SubmitForReviewAsync(
        int carId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        // 1. 车辆不存在
        if (car == null)
            throw new CarNotFoundException(carId);

        // 2. 不是车主
        // 为什么要检查这个？如果不检查，任何登录用户都能提交别人的车去审核，
        // 卖家会莫名其妙发现自己的草稿进入了审核状态
        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        // 3. 不是 Draft 状态
        // 为什么要检查这个？防止重复提交。已经在 PendingReview 的车再次提交没有意义，
        // Published 的车更不应该被重新提交审核（会破坏状态机）
        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.PendingReview;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        _logger.LogInformation("Car {CarId} submitted for review by seller {SellerId}",
            car.Id, currentUserId);

        return MapToResponse(updated);
    }

    public async Task<CarResponse> UpdateAsync(
        int carId,
        int currentUserId,
        CarUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只有车主可以修改
        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        // 只有 Draft 状态可以修改
        // PendingReview：正在审核中，修改会导致审核内容和实际内容不一致
        // Published：已上架，买家正在查看，不允许随意修改
        // Sold / Deleted：已完成或已删除，修改没有意义
        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

        car.Title = request.Title;
        car.Brand = request.Brand;
        car.Model = request.Model;
        car.Year = request.Year;
        car.Price = request.Price;
        car.Mileage = request.Mileage;
        car.Description = request.Description;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        _logger.LogInformation("Car {CarId} updated by seller {SellerId}", car.Id, currentUserId);

        return MapToResponse(updated);
    }
    

    // 实体 → DTO 的映射方法
    // 注意 SellerUsername 暂时用空字符串——创建时 EF Core 不会自动加载导航属性
    // 后续详情接口会用 Include 加载完整的 Seller 信息
    internal static CarResponse MapToResponse(Car car) => new()
    {
        Id = car.Id,
        Title = car.Title,
        Brand = car.Brand,
        Model = car.Model,
        Year = car.Year,
        Price = car.Price,
        Mileage = car.Mileage,
        Description = car.Description,
        Status = car.Status.ToString(),
        SellerId = car.SellerId,
        SellerUsername = car.Seller?.Username ?? string.Empty,
        CreatedAt = car.CreatedAt,
        UpdatedAt = car.UpdatedAt
    };
}