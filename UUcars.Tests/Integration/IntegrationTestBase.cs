using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;

namespace UUcars.Tests.Integration;

// 管理每个集成测试共用的 Client、数据生命周期和辅助方法。
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiIntegrationTestFactory _factory;

    protected HttpClient Client { get; }

    protected IntegrationTestBase(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureTestAdminAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _factory.CleanDatabaseAsync();
        }
        finally
        {
            Client.Dispose();
        }
    }

    protected AppDbContext CreateDbContext()
    {
        return _factory.CreateDbContext();
    }

    // 构建 JSON 请求体
    protected static StringContent JsonContent(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    // 反序列化 HTTP 响应体
    protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    // 通过真实 API 完成注册、邮箱确认和登录，返回 UUcars JWT。
    protected async Task<string> RegisterAndLoginAsync(
        string email = "test@example.com",
        string username = "testuser",
        string password = "Test@123456")
    {
        await Client.PostAsync("/auth/register", JsonContent(new
        {
            username, email, password
        }));

        // 后台邮件任务不会在集成测试中执行，因此直接读取已经保存的验证 Token。
        await using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        var verifyToken = user!.EmailConfirmationToken;

        await Client.GetAsync($"/auth/verify-email?token={verifyToken}");

        var loginResponse = await Client.PostAsync("/auth/login", JsonContent(new
        {
            email, password
        }));

        var result = await DeserializeAsync<ApiResponse<LoginData>>(loginResponse);
        return result?.Data?.Token ?? string.Empty;
    }

    // 设置请求头的 Bearer Token
    protected void SetBearerToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // 清除 Token（测试未登录场景时使用）
    protected void ClearBearerToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    // ===== 内部响应结构 =====

    protected class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }

    protected class LoginData
    {
        public string Token { get; set; } = string.Empty;
    }

    protected class UserData
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    protected class CarData
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    protected class PagedData<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    protected class OrderData
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int SellerId { get; set; }
    }
}
