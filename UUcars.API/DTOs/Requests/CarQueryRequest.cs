using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarQueryRequest
{
    // 页码，默认第 1 页
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;  

    // 每页条数，默认 20 条
    [Range(1, 1000)] // 给个宽一点范围
    public int PageSize { get; set; } = 20; 
    
    
    // 以下字段都是可选的（nullable），不传就不过滤
    // 为什么用 string? 而不是 string？
    // 过滤条件是可选的——不传 Brand 表示"不限品牌"，
    // 用 nullable 类型，Service 层可以用 null 判断是否需要应用这个过滤条件

    // 品牌过滤：精确匹配（BMW、Toyota 等）
    public string? Brand { get; set; }

    // 价格区间：minPrice / maxPrice 都是可选的，
    // 可以只传其中一个（比如"最高 20 万"只传 maxPrice）
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // 年份区间：和价格区间同理
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
}