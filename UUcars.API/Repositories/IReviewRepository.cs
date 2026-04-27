using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IReviewRepository
{
    // 检查某订单是否已经评价过
    Task<bool> ExistsByOrderIdAsync(int orderId,
        CancellationToken cancellationToken = default);

    // 创建评价
    Task<Review> AddAsync(Review review,
        CancellationToken cancellationToken = default);

    // 查询某卖家的所有评价
    Task<List<Review>> GetByRevieweeIdAsync(int revieweeId,
        CancellationToken cancellationToken = default);
}