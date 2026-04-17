using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.Tests.Integration;

// 自定义的 WebApplicationFactory
// 在测试启动时自动拉起 SQL Server 容器，测试结束后自动销毁
public class SqlServerTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // MsSqlContainer：Testcontainers 提供的 SQL Server 容器对象
    // 它封装了 Docker 操作：启动容器、获取连接字符串、停止容器
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("TestStrong!Passw0rd")
        .Build();

    // IAsyncLifetime.InitializeAsync：测试类实例化时自动调用
    // xUnit 发现这个接口后，会在运行第一个测试前先执行这个方法
    public async Task InitializeAsync()
    {
        // 启动 SQL Server 容器（需要几秒钟）
        await _sqlContainer.StartAsync();
    }

    // IAsyncLifetime.DisposeAsync：所有测试结束后自动调用
    // 停止并销毁容器
    public new async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
    }

    // ConfigureWebHost：在 WebApplicationFactory 构建应用时调用
    // 在这里替换 DI 服务，把生产用的 SQL Server 换成测试容器
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 找到并移除原来注册的 DbContext（连接生产数据库的）
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // 用测试容器的连接字符串注册新的 DbContext
            // _sqlContainer.GetConnectionString() 返回容器的动态连接字符串
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));
        });

        // 使用测试环境，避免加载生产配置
        builder.UseEnvironment("Testing");
    }

    // 初始化数据库：执行 Migration + 插入初始数据
    // 在每个测试类开始前调用，确保数据库结构是最新的
    public async Task InitializeDatabaseAsync()
    {
        // 通过 DI 容器获取 DbContext
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 执行所有 Migration，建好数据库表结构
        await context.Database.MigrateAsync();

        // 插入测试需要的 Admin 用户
        // 为什么要手动插入？
        // Step 07 里的 Seed Data（HasData）会在 Migration 文件里生成 INSERT 语句，
        // MigrateAsync 执行时会自动插入。但如果 Seed Data 已经在 Migration 里了，
        // 这里就不需要手动插了。如果没有，就在这里手动创建
        if (!context.Users.Any(u => u.Role == UserRole.Admin))
        {
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Username = "admin",
                Email = "admin@uucars.com",
                Role = UserRole.Admin,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@123456");
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }

    // 清理数据库：每个测试方法结束后调用
    // 删除所有业务数据，但保留表结构和 Admin 账号
    // 这样下一个测试面对的是干净的数据库
    public async Task CleanDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 按外键依赖顺序删除，避免约束冲突
        context.Favorites.RemoveRange(context.Favorites);
        context.Orders.RemoveRange(context.Orders);
        context.CarImages.RemoveRange(context.CarImages);
        context.Cars.RemoveRange(context.Cars);
        // 只删除普通用户，保留 Admin
        context.Users.RemoveRange(context.Users.Where(u => u.Role != UserRole.Admin));
        await context.SaveChangesAsync();
    }
}