namespace UUcars.API.DTOs.Responses;

// 卖家评价汇总：平均分 + 评价列表
public class SellerRatingResponse
{
    public int SellerId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<ReviewResponse> Reviews { get; set; } = [];
}