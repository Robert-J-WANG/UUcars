using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UUcars.Tests.Integration;

// 集成测试基类
// 提供统一的 HTTP 客户端和辅助方法
public abstract class IntegrationTestBase : IAsyncLifetime
{
    // JSON 选项：反序列化时忽略大小写
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected readonly HttpClient Client;

    // 共享同一个 SqlServerTestFactory（共享同一个数据库容器）
    protected readonly SqlServerTestFactory Factory;

    protected IntegrationTestBase(SqlServerTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    // 每个测试方法开始前：初始化数据库（只在第一次时执行 Migration）
    public async Task InitializeAsync()
    {
        await Factory.InitializeDatabaseAsync();
    }

    // 每个测试方法结束后：清理数据，为下一个测试准备干净环境
    public async Task DisposeAsync()
    {
        await Factory.CleanDatabaseAsync();
    }

    // ===== 辅助方法 =====

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

    // 注册用户并登录，返回 JWT Token
    protected async Task<string> RegisterAndLoginAsync(
        string email = "test@example.com",
        string username = "testuser",
        string password = "Test@123456")
    {
        await Client.PostAsync("/auth/register", JsonContent(new
        {
            username, email, password
        }));

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