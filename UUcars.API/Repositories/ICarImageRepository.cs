using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarImageRepository
{
    Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default);


    Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default);
    Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default);

    // ✅ 新增：批量添加，一次性插入多张图片，同一个事务里完成
    Task<List<CarImage>> AddRangeAsync(List<CarImage> images, CancellationToken cancellationToken = default);

    // ✅ 新增：根据车辆 Id 查询该车辆的所有图片
    // 批量上传时用于计算已有数量
    Task<List<CarImage>> GetByCarIdAsync(int carId, CancellationToken cancellationToken = default);

    // ✅ 新增：批量更新排序
    Task UpdateSortOrdersAsync(
        List<CarImage> images, CancellationToken cancellationToken = default);
}