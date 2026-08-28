using System.Net;
using Microsoft.EntityFrameworkCore;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

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

        // V3 更新：直接从数据库取 Token
        await using var db = Factory.GetDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");
        var verifyToken = user!.EmailConfirmationToken;

        await Client.GetAsync($"/auth/verify-email?token={verifyToken}");

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

    // ===== Admin Dashboard =====

    [Fact]
    public async Task GetAdminStats_AsAdmin_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var nowUtc = DateTime.UtcNow;
        var todayUtc = nowUtc.Date;

        var startOfMonth = new DateTime(
            nowUtc.Year,
            nowUtc.Month,
            1,
            0, 0, 0,
            DateTimeKind.Utc);

        var startOfNextMonth = startOfMonth.AddMonths(1);

        await using var db = Factory.GetDbContext();

        var seller = new User
        {
            Username = "stats-seller",
            Email = "stats-seller@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        var buyer = new User
        {
            Username = "stats-buyer",
            Email = "stats-buyer@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        db.Users.AddRange(seller, buyer);
        await db.SaveChangesAsync();

        // Published + Sold 共 11 个品牌。
        // Toyota 有 3 辆，其余品牌各 1 辆。
        // Top 10 排序后，ZzzTestBrand 应被排除。
        var marketCarDefinitions = new (string Brand, CarStatus Status)[]
        {
            ("Toyota", CarStatus.Published),
            ("Toyota", CarStatus.Published),
            ("Toyota", CarStatus.Sold),
            ("Mazda", CarStatus.Published),
            ("Honda", CarStatus.Published),
            ("Ford", CarStatus.Published),
            ("BMW", CarStatus.Published),
            ("Audi", CarStatus.Published),
            ("Nissan", CarStatus.Published),
            ("Hyundai", CarStatus.Published),
            ("Kia", CarStatus.Published),
            ("Suzuki", CarStatus.Published),
            ("ZzzTestBrand", CarStatus.Published)
        };

        var marketCars = marketCarDefinitions
            .Select((item, index) => new Car
            {
                Title = $"{item.Brand} test car {index + 1}",
                Brand = item.Brand,
                Model = "Test Model",
                Year = 2020,
                Price = 10000m,
                Mileage = 50000,
                SellerId = seller.Id,
                Status = item.Status,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            })
            .ToList();

        var pendingCar = new Car
        {
            Title = "Pending test car",
            Brand = "PendingBrand",
            Model = "Test Model",
            Year = 2020,
            Price = 10000m,
            Mileage = 50000,
            SellerId = seller.Id,
            Status = CarStatus.PendingReview,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        var draftCar = new Car
        {
            Title = "Draft test car",
            Brand = "DraftBrand",
            Model = "Test Model",
            Year = 2020,
            Price = 10000m,
            Mileage = 50000,
            SellerId = seller.Id,
            Status = CarStatus.Draft,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        var deletedCar = new Car
        {
            Title = "Deleted test car",
            Brand = "DeletedBrand",
            Model = "Test Model",
            Year = 2020,
            Price = 10000m,
            Mileage = 50000,
            SellerId = seller.Id,
            Status = CarStatus.Deleted,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        db.Cars.AddRange(marketCars);
        db.Cars.AddRange(pendingCar, draftCar, deletedCar);
        await db.SaveChangesAsync();

        var soldCar = marketCars.Single(car => car.Status == CarStatus.Sold);

        db.Orders.AddRange(
            // 本月第一刻创建：计入 monthlyOrders。
            // 本月完成：计入 monthlyRevenue。
            new Order
            {
                CarId = soldCar.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                Price = 10000m,
                Status = OrderStatus.Completed,
                CreatedAt = startOfMonth,
                UpdatedAt = nowUtc
            },

            // Pending 订单计入 monthlyOrders，
            // 但不计入 monthlyRevenue。
            new Order
            {
                CarId = soldCar.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                Price = 5000m,
                Status = OrderStatus.Pending,
                CreatedAt = startOfMonth,
                UpdatedAt = nowUtc
            },

            // 上月创建，不计入 monthlyOrders；
            // 本月完成，所以计入 monthlyRevenue。
            new Order
            {
                CarId = soldCar.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                Price = 20000m,
                Status = OrderStatus.Completed,
                CreatedAt = startOfMonth.AddTicks(-1),
                UpdatedAt = nowUtc
            },

            // 下月第一刻创建并完成，
            // 不进入当前月份的订单量和成交额。
            new Order
            {
                CarId = soldCar.Id,
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                Price = 40000m,
                Status = OrderStatus.Completed,
                CreatedAt = startOfNextMonth,
                UpdatedAt = startOfNextMonth
            });

        await db.SaveChangesAsync();

        // 使用测试数据库中已经存在的 Admin 登录
        var loginResponse = await Client.PostAsync(
            "/auth/login",
            JsonContent(new
            {
                email = "admin@uucars.com",
                password = "Admin@123456"
            }));

        var adminToken =
            (await DeserializeAsync<ApiResponse<LoginData>>(loginResponse))
            ?.Data?.Token;

        SetBearerToken(adminToken!);

        // Act
        var response = await Client.GetAsync("/admin/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await DeserializeAsync<ApiResponse<AdminStatsResponse>>(response);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        var stats = result.Data;

        // 13 辆 Published/Sold + Pending + Draft；
        // Deleted 不计入 totalCars。
        Assert.Equal(15, stats.TotalCars);
        Assert.Equal(1, stats.PendingCars);
        Assert.Equal(12, stats.PublishedCars);

        // 测试数据库保留 1 个 Admin，再加入 seller 和 buyer。
        Assert.Equal(3, stats.TotalUsers);

        // 当前月创建了 2 个订单；
        // 当前月完成金额为 10000 + 20000。
        Assert.Equal(2, stats.MonthlyOrders);
        Assert.Equal(30000m, stats.MonthlyRevenue);

        // Service 应返回连续的最近 30 个 UTC 日期。
        Assert.Equal(30, stats.DailyNewCars.Count);
        Assert.Equal(30, stats.DailyRevenue.Count);
        Assert.Equal(
            DateOnly.FromDateTime(todayUtc.AddDays(-29)),
            stats.DailyNewCars[0].Date);
        Assert.Equal(
            DateOnly.FromDateTime(todayUtc),
            stats.DailyNewCars[^1].Date);

        // 所有 16 辆车都在今天创建。
        // dailyNewCars 记录创建事件，因此 Deleted 也包含在内。
        Assert.Equal(
            16,
            stats.DailyNewCars.Single(item => item.Date == DateOnly.FromDateTime(todayUtc)).Count);

        // 两个 Completed 订单在今天完成。
        Assert.Equal(
            30000m,
            stats.DailyRevenue.Single(item => item.Date == DateOnly.FromDateTime(todayUtc)).Revenue);

        // 品牌只返回前 10。
        Assert.Equal(10, stats.BrandDistribution.Count);
        Assert.Equal("Toyota", stats.BrandDistribution[0].Brand);
        Assert.Equal(3, stats.BrandDistribution[0].Count);
        Assert.DoesNotContain(
            stats.BrandDistribution,
            item => item.Brand == "ZzzTestBrand");
    }

    [Fact]
    public async Task GetAdminStats_WithNonAdminToken_ShouldReturn403()
    {
        // Arrange
        var userToken = await RegisterAndLoginAsync(
            "stats-user@example.com",
            "stats-user");

        SetBearerToken(userToken);

        // Act
        var response = await Client.GetAsync("/admin/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}