using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarQueryRequest
{
    // 页码，默认第 1 页
    // 为什么用属性而不是方法参数？
    // [FromQuery] 绑定时，框架会把 Query 参数自动映射到这个类的属性上，
    // 比 Action 方法上写一堆参数更清晰
    
    // 页码，默认第 1 页
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;  

    // 每页条数，默认 20 条
    [Range(1, 1000)] // 给个宽一点范围
    public int PageSize { get; set; } = 20; 
}