using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class ReviewService
{
    private readonly ILogger<ReviewService> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository,
        ILogger<ReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<ReviewResponse> CreateAsync(
        int reviewerId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 验证订单存在
        var order = await _orderRepository.GetByIdAsync(
            request.OrderId, cancellationToken);

        if (order == null)
            throw new OrderNotFoundException(request.OrderId);

        // 2. 只有买家可以评价
        // 卖家不能评价自己的订单，也不能让第三方评价
        if (order.BuyerId != reviewerId)
            throw new ForbiddenException();

        // 3. 只有 Completed 状态的订单可以评价
        // 确保交易真实完成后才能评价，防止虚假评价
        if (order.Status != OrderStatus.Completed)
            throw new OrderNotCompletedException();

        // 4. 检查是否已经评价过
        // 数据库有唯一索引兜底，这里提前检查给出更友好的错误信息
        var alreadyReviewed = await _reviewRepository
            .ExistsByOrderIdAsync(request.OrderId, cancellationToken);
        if (alreadyReviewed)
            throw new ReviewAlreadyExistsException();

        // 5. 创建评价
        // RevieweeId 从订单里取，不从客户端传入
        // 防止客户端传入错误的被评价人
        var review = new Review
        {
            OrderId = request.OrderId,
            ReviewerId = reviewerId,
            RevieweeId = order.SellerId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _reviewRepository.AddAsync(review, cancellationToken);

        _logger.LogInformation(
            "Review created for order {OrderId} by buyer {ReviewerId}",
            request.OrderId, reviewerId);

        return new ReviewResponse
        {
            Id = created.Id,
            OrderId = created.OrderId,
            ReviewerId = created.ReviewerId,
            ReviewerUsername = order.Buyer?.Username ?? string.Empty,
            Rating = created.Rating,
            Comment = created.Comment,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<SellerRatingResponse> GetSellerRatingAsync(
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _reviewRepository
            .GetByRevieweeIdAsync(sellerId, cancellationToken);

        // 平均分：没有评价时返回 0
        var averageRating = reviews.Count > 0
            ? reviews.Average(r => r.Rating)
            : 0;

        return new SellerRatingResponse
        {
            SellerId = sellerId,
            AverageRating = Math.Round(averageRating, 1),
            TotalReviews = reviews.Count,
            Reviews = reviews.Select(r => new ReviewResponse
            {
                Id = r.Id,
                OrderId = r.OrderId,
                ReviewerId = r.ReviewerId,
                ReviewerUsername = r.Reviewer?.Username ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }
}