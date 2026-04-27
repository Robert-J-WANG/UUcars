namespace UUcars.API.DTOs.Requests;

// V2 更新：从接收 URL 改为接收文件
// 不用 [FromBody]，文件上传用 [FromForm]
// IFormFile 是 ASP.NET Core 提供的接口，封装了上传文件的所有信息
public class CarImageAddRequest
{
    public IFormFile File { get; set; } = null!;
    public int SortOrder { get; set; } = 0;
}