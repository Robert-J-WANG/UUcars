using UUcars.API.DTOs;
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
    private readonly ICarImageRepository _carImageRepository;
    private readonly ILogger<CarService> _logger;

    public CarService(ICarRepository carRepository,ICarImageRepository carImageRepository ,ILogger<CarService> logger)
    {
        _carRepository = carRepository;
        _carImageRepository = carImageRepository;
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
        // Published 的车更不应该被重新提交审核:会破坏状态机
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

    public async Task DeleteAsync(int carId,int currentUserId, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);
        if (car == null)
            throw new CarNotFoundException(carId);
        if (car.SellerId != currentUserId)
            throw new ForbiddenException();
        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);
        car.Status = CarStatus.Deleted;
        car.UpdatedAt = DateTime.UtcNow;
        await _carRepository.UpdateAsync(car, cancellationToken);
        
        _logger.LogInformation("Car {CarId} deleted by seller {SellerId}", car.Id, currentUserId);
    }


    public async Task<CarImageResponse> AddImageAsync(
        int carId,
        int currentUserId,
        CarImageAddRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        // 只有 Draft 状态才能添加图片
        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

        var image = new CarImage
        {
            CarId = carId,
            ImageUrl = request.ImageUrl,
            SortOrder = request.SortOrder
        };

        var created = await _carImageRepository.AddAsync(image, cancellationToken);

        _logger.LogInformation("Image added to car {CarId} by seller {SellerId}", carId, currentUserId);

        return MapToImageResponse(created);
    }

    public async Task DeleteImageAsync(
        int carId,
        int imageId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 先验证车辆存在（保证 carId 是合法的）
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);
        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

        // 再验证图片存在，且属于这辆车
        // 为什么要检查 image.CarId == carId？
        // 防止用户构造 /cars/1/images/99 这样的请求来删除属于 car 99 的图片
        // URL 里的 carId 和图片实际的 CarId 必须匹配
        var image = await _carImageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image == null || image.CarId != carId)
            throw new CarImageNotFoundException(imageId);

        await _carImageRepository.DeleteAsync(image, cancellationToken);

        _logger.LogInformation("Image {ImageId} deleted from car {CarId} by seller {SellerId}",
            imageId, carId, currentUserId);
    }


    public async Task<PagedResponse<CarResponse>> GetPublishedCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {

        var page = query.Page;
        // 只做上限保护（业务规则）
        // 单页最多返回 50 条，防止客户端传 pageSize=99999 把服务器打垮
        var pageSize = Math.Min(query.PageSize, 50);

        var (cars, totalCount) = await _carRepository.GetPagedAsync(
            CarStatus.Published,
            page,
            pageSize,
            cancellationToken);

        var items = cars.Select(MapToResponse).ToList();

        // PagedResponse.Create 会自动计算 TotalPages
        return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
    }

    public async Task<PagedResponse<CarResponse>> GetSellerCarsAsync(int sellerId, CarQueryRequest query, CancellationToken cancellationToken = default)
    {
        var page = query.Page;
        var pageSize = Math.Min(50, query.PageSize);
        
        var (cars, totalCount) = await _carRepository.GetBySellerAsync(
            sellerId,
            page,
            pageSize,
            cancellationToken);

        var items = cars.Select(MapToResponse).ToList();

        return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
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

    internal static CarImageResponse MapToImageResponse(CarImage image) => new()
    {
        Id = image.Id,
        ImageUrl = image.ImageUrl,
        SortOrder = image.SortOrder,
        CarId = image.CarId
    };
}