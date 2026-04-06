using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
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

        _logger.LogInformation("Car created: {CarId} by seller {SellerId}", created.Id, sellerId);

        return MapToResponse(created);
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