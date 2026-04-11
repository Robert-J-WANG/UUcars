using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class OrderCreateRequest
{
    // 买家只需要传 CarId，其他信息（Price、SellerId）由服务端从车辆信息里取
    // 不让客户端传 Price，防止客户端篡改价格
    [Required(ErrorMessage = "CarId is required.")]
    public int CarId { get; set; }
}