namespace UUcars.API.DTOs;

// 专门用于列表接口的分页响应
// 使用时嵌套在 ApiResponse 里：ApiResponse<PagedResponse<CarResponse>>
public class PagedResponse<T>
{
    public T? Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    // TotalPages 根据 TotalCount 和 PageSize 自动计算，不需要调用方传入
    public static PagedResponse<T> Create(T items, int totalCount, int page, int pageSize) =>
        new()
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
}