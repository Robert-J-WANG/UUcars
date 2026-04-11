using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class FavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        ICarRepository carRepository,
        ILogger<FavoriteService> logger)
    {
        _favoriteRepository = favoriteRepository;
        _carRepository = carRepository;
        _logger = logger;
    }

    public async Task<FavoriteResponse> AddFavoriteAsync(
        int userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        // 1. 验证车辆存在且是 Published 状态
        // 这里用 GetByIdAsync（不用 GetDetailByIdAsync），
        // 因为不需要加载图片列表，只需要验证车辆状态
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只能收藏 Published 状态的车辆
        if (car.Status != CarStatus.Published)
            throw new CarNotFoundException(carId); // 对用户表现为"不存在"，原因同详情接口

        // 2. 检查是否已收藏
        var existing = await _favoriteRepository.GetAsync(userId, carId, cancellationToken);
        if (existing != null)
            throw new AlreadyFavoritedException(carId);

        // 3. 创建收藏记录
        var favorite = new Favorite
        {
            UserId = userId,
            CarId = carId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _favoriteRepository.AddAsync(favorite, cancellationToken);

        _logger.LogInformation("User {UserId} favorited car {CarId}", userId, carId);

        return new FavoriteResponse
        {
            CarId = created.CarId,
            UserId = created.UserId,
            CreatedAt = created.CreatedAt,
            Car = CarService.MapToResponse(car)
        };
    }

    public async Task RemoveFavoriteAsync(int userId, int carId, CancellationToken cancellationToken = default)
    {
        var favorite = await _favoriteRepository.GetAsync(userId, carId, cancellationToken);

        if (favorite == null) throw new FavoriteNotFoundException(carId);

        await _favoriteRepository.DeleteAsync(favorite, cancellationToken);
        _logger.LogInformation("User {UserId} removed car {CarId} from favorites", userId, carId);
    }

    public async Task<PagedResponse<FavoriteResponse>> GetMyFavoritesAsync(
        int userId,
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        var (favorites, totalCount) = await _favoriteRepository.GetByUserAsync(
            userId,
            page,
            pageSize,
            cancellationToken);

        var items = favorites.Select(f => new FavoriteResponse
        {
            CarId = f.CarId,
            UserId = f.UserId,
            CreatedAt = f.CreatedAt,
            // f.Car 在 EfFavoriteRepository 里已经通过 Include + ThenInclude 加载好了
            Car = CarService.MapToResponse(f.Car)
        }).ToList();

        return PagedResponse<FavoriteResponse>.Create(items, totalCount, page, pageSize);
    }
}