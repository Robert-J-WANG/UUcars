namespace UUcars.API.DTOs.Requests;

// 单张图片的新排序值
public class CarImageOrderItem
{
    public int ImageId { get; set; }
    public int SortOrder { get; set; }
}

// 排序请求：一次性提交整组图片的新顺序
public class CarImageReorderRequest
{
    public List<CarImageOrderItem> Items { get; set; } = [];
}