using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IFavoriteRepository
{
    // 查询某用户是否已收藏某辆车
    Task<Favorite?> GetAsync(int userId, int carId, CancellationToken cancellationToken = default);

    // 添加收藏
    Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);

    // 删除收藏
    Task DeleteAsync(Favorite favorite, CancellationToken cancellationToken = default);

    // 获取某用户的所有收藏
    Task<(List<Favorite> Favorites, int TotalCount)> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}