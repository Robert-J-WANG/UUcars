using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using UUcars.API.Configurations;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Services.BackgroundJobs;
using UUcars.API.Services.Cache;
using UUcars.API.Services.Email;
using UUcars.API.Services.ExternalLogin;
using UUcars.API.Services.Storage;
using UUcars.Tests.Fakes;
using UUcars.Tests.Services;

namespace UUcars.Tests.Integration;

// 创建并管理整套 API 集成测试环境。
public class ApiIntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("TestStrong!Passw0rd")
        .Build();

    // 整个 Integration Collection 开始前启动一次临时 SQL Server。
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    // 整个 Integration Collection 完成后停止临时 SQL Server。
    public new async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(AppEnvironments.Testing);

        builder.ConfigureServices(services =>
        {
            ConfigureTestDatabase(services);
            ReplaceExternalServices(services);
            ConfigureTestRateLimiting(services);
        });
    }

    // 为测试或测试辅助方法创建指向临时数据库的 DbContext。
    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    // Program.cs 已在 Test Host 启动时执行 Migration；这里仅准备测试需要的 Admin。
    public async Task EnsureTestAdminAsync()
    {
        await using var context = CreateDbContext();

        if (await context.Users.AnyAsync(user => user.Role == UserRole.Admin))
            return;

        var admin = new User
        {
            Username = "admin",
            Email = "admin@uucars.com",
            Role = UserRole.Admin,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123456");

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }

    // 每个测试完成后删除业务数据，保留数据库结构和 Admin 测试账号。
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext();

        context.Favorites.RemoveRange(context.Favorites);
        context.Orders.RemoveRange(context.Orders);
        context.CarImages.RemoveRange(context.CarImages);
        context.Cars.RemoveRange(context.Cars);
        context.ExternalLogins.RemoveRange(context.ExternalLogins);
        context.Users.RemoveRange(context.Users.Where(user => user.Role != UserRole.Admin));

        await context.SaveChangesAsync();
    }

    private void ConfigureTestDatabase(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(_sqlContainer.GetConnectionString()));
    }

    private static void ReplaceExternalServices(IServiceCollection services)
    {
        services.RemoveAll<IBackgroundJobService>();
        services.AddSingleton<IBackgroundJobService, FakeBackgroundJobService>();

        services.RemoveAll<ICacheService>();
        services.AddSingleton<ICacheService, FakeCacheService>();

        services.RemoveAll<IGoogleIdTokenService>();
        services.AddSingleton<IGoogleIdTokenService, FakeGoogleIdTokenService>();

        services.RemoveAll<IEmailService>();
        services.AddScoped<IEmailService, FakeEmailService>();

        services.RemoveAll<IStorageService>();
        services.AddScoped<IStorageService, FakeStorageService>();
    }

    private static void ConfigureTestRateLimiting(IServiceCollection services)
    {
        var rateLimiterDescriptors = services
            .Where(descriptor => descriptor.ServiceType.FullName?.Contains("RateLimit") == true)
            .ToList();

        foreach (var descriptor in rateLimiterDescriptors)
            services.Remove(descriptor);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetNoLimiter("no-limit"));

            foreach (var policyName in new[]
                     {
                         "login", "register", "forgot-password", "resend-verification", "browse", "write"
                     })
                options.AddPolicy(policyName, _ =>
                    RateLimitPartition.GetNoLimiter("no-limit"));
        });
    }
}
