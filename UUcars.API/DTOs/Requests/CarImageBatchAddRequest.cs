namespace UUcars.API.DTOs.Requests;

// 批量图片上传请求
// IFormFileCollection：对应 multipart/form-data 里多个同名文件字段
// 前端用同一个字段名（"files"）多次 append，后端自动收集成这个集合
public class CarImageBatchAddRequest
{
    public IFormFileCollection Files { get; set; } = null!;
}