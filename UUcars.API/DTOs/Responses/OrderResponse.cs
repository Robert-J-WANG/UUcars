namespace UUcars.API.DTOs.Responses;

public class OrderResponse
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public string CarTitle { get; set; } = string.Empty;
    public int BuyerId { get; set; }
    public string BuyerUsername { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;

    // 锁定的成交价格（创建订单时从车辆复制过来）
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}