using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services.Cache;
using UUcars.API.Services.Storage;

namespace UUcars.API.Services;

public class CarService
{
    private readonly ICacheService _cache; // ✅ 新增

    private readonly ICarImageRepository _carImageRepository;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<CarService> _logger;

    // 构造函数新增 IStorageService
    private readonly IStorageService _storageService;

    public CarService(ICacheService cache, ICarImageRepository carImageRepository, ICarRepository carRepository,
        ILogger<CarService> logger, IStorageService storageService)
    {
        _cache = cache; // ✅ 新增
        _carImageRepository = carImageRepository;
        _carRepository = carRepository;
        _logger = logger;
        _storageService = storageService;
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
            SellerId = sellerId, // 从 Token 里取到的当前用户 Id
            Status = CarStatus.Draft, // 创建时强制为 Draft，客户端无法指定
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _carRepository.AddAsync(car, cancellationToken);
        // 再查一次，把 Seller 带出来


        _logger.LogInformation("Car created: {CarId} by seller {SellerId}", created.Id, sellerId);

        // Create 完之后，返回里必须要有 SellerUsername的话，Add 完 → 再查一次（因为GetByIdAsync带 Include， 手动加载car.Seller（导航属性）），这样car.Seller就不是null了
        var carWithSeller = await _carRepository.GetByIdAsync(created.Id, cancellationToken);
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

        //清除待审核车辆列表缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

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

    public async Task DeleteAsync(int carId, int currentUserId, CancellationToken cancellationToken = default)
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

        // 验证文件
        var (isValid, error) = FileValidator.Validate(request.File);
        if (!isValid)
            throw new AppException(StatusCodes.Status400BadRequest, error!);

        // 生成唯一文件名，上传到 R2
        var fileName = FileValidator.GenerateFileName(request.File.FileName);

        string imageUrl;
        await using (var stream = request.File.OpenReadStream())
        {
            imageUrl = await _storageService.UploadAsync(
                stream, fileName, request.File.ContentType, cancellationToken);
        }

        var image = new CarImage
        {
            CarId = carId,
            ImageUrl = imageUrl,
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

        // 从 URL 里提取文件名（格式：{PublicUrl}/cars/{guid}.jpg）
        // 只需要 "cars/{guid}.jpg" 这部分来删除 R2 里的文件
        var uri = new Uri(image.ImageUrl);
        var fileName = uri.AbsolutePath.TrimStart('/');

        await _storageService.DeleteAsync(fileName, cancellationToken);

        await _carImageRepository.DeleteAsync(image, cancellationToken);

        _logger.LogInformation("Image {ImageId} deleted from car {CarId} by seller {SellerId}",
            imageId, carId, currentUserId);
    }

    // ✅ 新增：批量添加图片
    public async Task<List<CarImageResponse>> AddImagesBatchAsync(
        int carId,
        int currentUserId,
        CarImageBatchAddRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

        // 数量上限校验：一辆车最多 10 张图
        // 在后端统一校验（而不是让前端自己算），避免前端并行上传时的竞态问题
        const int maxImagesPerCar = 10;
        var existingImages = await _carImageRepository.GetByCarIdAsync(carId, cancellationToken);
        if (existingImages.Count + request.Files.Count > maxImagesPerCar)
            throw new AppException(
                StatusCodes.Status400BadRequest,
                $"A car can have at most {maxImagesPerCar} images. " +
                $"Currently has {existingImages.Count}, attempted to add {request.Files.Count}.");

        // 逐一校验每个文件（大小、类型）
        // 任何一个文件不合法，整批都拒绝，不做"部分成功"
        foreach (var file in request.Files)
        {
            var (isValid, error) = FileValidator.Validate(file);
            if (!isValid)
                throw new AppException(StatusCodes.Status400BadRequest, error!);
        }

        // 新图片从当前最大 SortOrder 之后开始排
        var currentMaxSortOrder = existingImages.Count > 0
            ? existingImages.Max(i => i.SortOrder)
            : -1;

        // 逐个上传到 R2（R2/S3 协议本身不支持批量上传，仍需逐个调用）
        var newImages = new List<CarImage>();
        var sortOrder = currentMaxSortOrder + 1;

        foreach (var file in request.Files)
        {
            var fileName = FileValidator.GenerateFileName(file.FileName);

            string imageUrl;
            await using (var stream = file.OpenReadStream())
            {
                imageUrl = await _storageService.UploadAsync(
                    stream, fileName, file.ContentType, cancellationToken);
            }

            newImages.Add(new CarImage
            {
                CarId = carId,
                ImageUrl = imageUrl,
                SortOrder = sortOrder
            });

            sortOrder++;
        }

        // 一次性批量写入数据库（一个事务，不会出现部分成功）
        var created = await _carImageRepository.AddRangeAsync(newImages, cancellationToken);

        _logger.LogInformation(
            "{Count} images added to car {CarId} by seller {SellerId}",
            created.Count, carId, currentUserId);

        return created.Select(MapToImageResponse).ToList();
    }

    // ✅ 新增：调整图片排序
    public async Task<List<CarImageResponse>> ReorderImagesAsync(int carId,
        int currentUserId,
        CarImageReorderRequest request,
        CancellationToken cancellationToken = default)
    {
        //基础权限校验
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);
        if (car == null) throw new CarNotFoundException(carId);
        if (car.SellerId != currentUserId) throw new ForbiddenException();
        if (car.Status != CarStatus.Draft) throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

        // 取出这辆车的所有图片，验证请求里的 ImageId 都属于这辆车
        // IDOR 防护：防止用户把别人车辆的 ImageId 塞进来
        var existingImages = await _carImageRepository.GetByCarIdAsync(carId, cancellationToken);
        var existingImageIds = existingImages.Select(i => i.Id).ToList();
        foreach (var item in request.Items)
            if (!existingImageIds.Contains(item.ImageId))
                throw new CarImageNotFoundException(item.ImageId);

        // 找到图片，并改成新的排序值
        foreach (var item in request.Items)
        {
            var image = existingImages.First(i => i.Id == item.ImageId);
            image.SortOrder = item.SortOrder;
        }

        // 统一批量写回
        await _carImageRepository.UpdateSortOrdersAsync(existingImages, cancellationToken);
        _logger.LogInformation(
            "Images reordered for car {CarId} by seller {SellerId}", carId, currentUserId);

        // 把数据按新的顺序返回
        return existingImages.OrderBy(i => i.SortOrder).Select(MapToImageResponse).ToList();
    }


// ✅ 公开车辆列表：加缓存
    public async Task<PagedResponse<CarResponse>> GetPublishedCarsAsync(
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Min(request.PageSize < 1 ? 20 : request.PageSize, 50);

        // 构建缓存 Key（包含所有过滤参数，不同参数对应不同缓存）
        var cacheKey = CacheKeys.PublishedCars(page, pageSize, request.Brand, request.MinPrice, request.MaxPrice,
            request.MinYear, request.MaxYear);

        // GetOrSetAsync：有缓存直接返回；没有则查数据库并缓存结果
        return await _cache.GetOrSetAsync(cacheKey, async () =>
            {
                // 这个 lambda 只在缓存未命中时执行
                var (cars, totalCount) = await _carRepository.GetPagedAsync(
                    CarStatus.Published,
                    request,
                    cancellationToken);

                var items = cars.Select(MapToResponse).ToList();

                // PagedResponse.Create 会自动计算 TotalPages
                return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
            },
            TimeSpan.FromSeconds(60), // 公开列表缓存 60 秒
            cancellationToken);
    }

    public async Task<PagedResponse<CarResponse>> GetSellerCarsAsync(int sellerId, CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cars, totalCount) = await _carRepository.GetBySellerAsync(
            sellerId,
            request,
            cancellationToken);

        var items = cars.Select(MapToResponse).ToList();

        return PagedResponse<CarResponse>.Create(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CarDetailResponse> GetDetailAsync(
        int carId,
        int? currentUserId, // nullable：未登录时为 null
        bool isAdmin, // 是否是 Admin 角色
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetDetailByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // Published 的车是公开资源，任何人都可以直接查看，不需要权限判断
        if (car.Status == CarStatus.Published)
            return MapToDetailResponse(car);

        // 非 Published 的车是受保护资源，需要验证身份
        // Admin 可以查看任何状态的车辆
        if (isAdmin)
            return MapToDetailResponse(car);

        // 车主可以查看自己发布的车辆（Draft / PendingReview 等）

        if (currentUserId.HasValue && car.SellerId == currentUserId.Value && car.Status != CarStatus.Deleted)
            return MapToDetailResponse(car);

        // 既不是 Published，也没有对应权限，对外表现为"不存在"
        throw new CarNotFoundException(carId);
    }

// 实体 → DTO 的映射方法
// 注意 SellerUsername 暂时用空字符串——创建时 EF Core 不会自动加载导航属性
// 后续详情接口会用 Include 加载完整的 Seller 信息
    internal static CarResponse MapToResponse(Car car)
    {
        return new CarResponse
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
            UpdatedAt = car.UpdatedAt,
            CoverImageUrl = car.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
        };
    }

    internal static CarImageResponse MapToImageResponse(CarImage image)
    {
        return new CarImageResponse
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            SortOrder = image.SortOrder,
            CarId = image.CarId
        };
    }

// 详情实体 → DTO 的映射（包含图片列表）
    private static CarDetailResponse MapToDetailResponse(Car car)
    {
        return new CarDetailResponse
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
            Images = car.Images
                .OrderBy(i => i.SortOrder) // 按 SortOrder 排序，确保图片顺序正确
                .Select(i => new CarImageResponse
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    SortOrder = i.SortOrder,
                    CarId = i.CarId
                })
                .ToList(),
            CreatedAt = car.CreatedAt,
            UpdatedAt = car.UpdatedAt
        };
    }
}