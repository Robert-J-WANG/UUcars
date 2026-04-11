namespace UUcars.API.DTOs.Responses;

public class FavoriteResponse
{
    public int CarId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // 返回被收藏的车辆摘要信息，方便客户端直接展示
    // 而不需要再发一次 GET /cars/{id} 请求
    public CarResponse? Car { get; set; }
}