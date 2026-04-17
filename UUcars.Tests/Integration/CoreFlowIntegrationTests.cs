using System.Net;

namespace UUcars.Tests.Integration;

// [Collection] 让这个测试类使用共享的 SqlServerTestFactory
// 同一个 Collection 里的所有测试类共享同一个数据库容器
// 避免每个测试类都启动一个新容器（启动容器需要几秒钟，非常耗时）
[Collection("Integration")]
public class CoreFlowIntegrationTests : IntegrationTestBase
{
    public CoreFlowIntegrationTests(SqlServerTestFactory factory)
        : base(factory)
    {
    }

    // ===== 用户注册和登录 =====

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201()
    {
        var response = await Client.PostAsync("/auth/register", JsonContent(new
        {
            username = "testuser",
            email = "test@example.com",
            password = "Test@123456"
        }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await DeserializeAsync<ApiResponse<UserData>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("test@example.com", result.Data?.Email);
        Assert.Equal("User", result.Data?.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // 先注册一次
        await Client.PostAsync("/auth/register", JsonContent(new
        {
            username = "user1",
            email = "duplicate@example.com",
            password = "Test@123456"
        }));

        // 用同一个邮箱再注册
        var response = await Client.PostAsync("/auth/register", JsonContent(new
        {
            username = "user2",
            email = "duplicate@example.com",
            password = "Test@123456"
        }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        await Client.PostAsync("/auth/register", JsonContent(new
        {
            username = "testuser",
            email = "test@example.com",
            password = "Test@123456"
        }));

        var response = await Client.PostAsync("/auth/login", JsonContent(new
        {
            email = "test@example.com",
            password = "Test@123456"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeAsync<ApiResponse<LoginData>>(response);
        Assert.NotNull(result?.Data?.Token);
        Assert.NotEmpty(result!.Data!.Token);
    }

    // ===== 认证保护 =====

    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturn401()
    {
        // 验证 JWT 中间件确实在保护这个接口
        var response = await Client.GetAsync("/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturnUserInfo()
    {
        var token = await RegisterAndLoginAsync();
        SetBearerToken(token);

        var response = await Client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await DeserializeAsync<ApiResponse<UserData>>(response);
        Assert.True(result?.Success);
        Assert.Equal("test@example.com", result?.Data?.Email);
    }

    // ===== 核心业务链路 =====

    [Fact]
    public async Task FullFlow_RegisterLoginCreateCarSubmit_ShouldWork()
    {
        // 这个测试验证完整的卖家操作链路：
        // 注册 → 登录 → 创建草稿 → 查看我的车辆 → 提交审核 → 验证公开列表

        // Step 1：注册并登录
        var token = await RegisterAndLoginAsync(
            "seller@example.com",
            "seller");
        SetBearerToken(token);

        // Step 2：创建车辆草稿
        var createResponse = await Client.PostAsync("/cars", JsonContent(new
        {
            title = "2020款宝马3系",
            brand = "BMW",
            model = "3 Series",
            year = 2020,
            price = 260000,
            mileage = 15000
        }));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var car = (await DeserializeAsync<ApiResponse<CarData>>(createResponse))?.Data;
        Assert.Equal("Draft", car?.Status);

        // Step 3：卖家能在"我的车辆"列表看到草稿
        var myListResponse = await Client.GetAsync("/cars/my-listings");
        Assert.Equal(HttpStatusCode.OK, myListResponse.StatusCode);
        var myList = (await DeserializeAsync<ApiResponse<PagedData<CarData>>>(myListResponse))?.Data;
        Assert.Equal(1, myList?.TotalCount);

        // Step 4：提交审核
        var submitResponse = await Client.PostAsync($"/cars/{car!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitted = (await DeserializeAsync<ApiResponse<CarData>>(submitResponse))?.Data;
        Assert.Equal("PendingReview", submitted?.Status);

        // Step 5：公开列表里不应该出现这辆车（还未 Published）
        ClearBearerToken();
        var publicListResponse = await Client.GetAsync("/cars");
        var publicList = (await DeserializeAsync<ApiResponse<PagedData<CarData>>>(publicListResponse))?.Data;
        Assert.Equal(0, publicList?.TotalCount);
    }

    [Fact]
    public async Task AdminFlow_ApproveCarAndBuyerOrder_ShouldWork()
    {
        // 验证完整的平台业务闭环：
        // 卖家发车 → Admin 审核通过 → 买家下单 → 车辆变 Sold → 买家取消 → 车辆恢复

        // Step 1：卖家创建并提交车辆
        var sellerToken = await RegisterAndLoginAsync(
            "seller@example.com",
            "seller");
        SetBearerToken(sellerToken);

        var createResponse = await Client.PostAsync("/cars", JsonContent(new
        {
            title = "2020款宝马3系", brand = "BMW", model = "3 Series",
            year = 2020, price = 260000, mileage = 15000
        }));
        var car = (await DeserializeAsync<ApiResponse<CarData>>(createResponse))?.Data;
        await Client.PostAsync($"/cars/{car!.Id}/submit", null);

        // Step 2：Admin 登录并审核通过
        // Admin 账号在 InitializeDatabaseAsync 里已经插入了
        var adminLoginResponse = await Client.PostAsync("/auth/login", JsonContent(new
        {
            email = "admin@uucars.com",
            password = "Admin@123456"
        }));
        var adminToken = (await DeserializeAsync<ApiResponse<LoginData>>(adminLoginResponse))?.Data?.Token;
        SetBearerToken(adminToken!);

        var approveResponse = await Client.PostAsync($"/admin/cars/{car.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // Step 3：验证车辆出现在公开列表
        ClearBearerToken();
        var publicListResponse = await Client.GetAsync("/cars");
        var publicList = (await DeserializeAsync<ApiResponse<PagedData<CarData>>>(publicListResponse))?.Data;
        Assert.Equal(1, publicList?.TotalCount);
        Assert.Equal("Published", publicList?.Items?.First().Status);

        // Step 4：买家注册登录并下单
        var buyerToken = await RegisterAndLoginAsync(
            "buyer@example.com",
            "buyer");
        SetBearerToken(buyerToken);

        var orderResponse = await Client.PostAsync("/orders", JsonContent(new
        {
            carId = car.Id
        }));
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = (await DeserializeAsync<ApiResponse<OrderData>>(orderResponse))?.Data;
        Assert.Equal("Pending", order?.Status);
        Assert.Equal(260000, order?.Price); // 价格已锁定

        // Step 5：下单后车辆从公开列表消失（Sold 状态）
        ClearBearerToken();
        var publicListAfterOrder = await Client.GetAsync("/cars");
        var publicListData = (await DeserializeAsync<ApiResponse<PagedData<CarData>>>(publicListAfterOrder))?.Data;
        Assert.Equal(0, publicListData?.TotalCount);

        // Step 6：买家取消订单，车辆恢复 Published
        SetBearerToken(buyerToken);
        var cancelResponse = await Client.PostAsync($"/orders/{order!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // Step 7：车辆重新出现在公开列表
        ClearBearerToken();
        var publicListRestored = await Client.GetAsync("/cars");
        var restoredData = (await DeserializeAsync<ApiResponse<PagedData<CarData>>>(publicListRestored))?.Data;
        Assert.Equal(1, restoredData?.TotalCount);
    }

    [Fact]
    public async Task AdminEndpoint_WithNonAdminToken_ShouldReturn403()
    {
        // 验证 Admin 接口的权限保护
        var userToken = await RegisterAndLoginAsync();
        SetBearerToken(userToken);

        var response = await Client.GetAsync("/admin/cars/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}