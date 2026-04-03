using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Data;

public class AppDbContext : DbContext
{
    // DbContextOptions 包含数据库连接字符串等配置
    // 通过构造函数注入，由 DI 容器在运行时传入
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet<T> 对应数据库里的一张表
    // 通过它可以对这张表做查询、新增、修改、删除
    public DbSet<User> Users => Set<User>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarImage> CarImages => Set<CarImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 统一加载 Configurations/ 目录下的所有配置类
        // 每新增一个 IEntityTypeConfiguration 实现，这里自动识别，不需要手动添加
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Seed Data：在数据库初始化时插入初始 Admin 用户
        // 为什么用固定时间（new DateTime(2025, 1, 1)）而不是 DateTime.UtcNow？
        // 因为 Seed Data 会被写入 Migration 文件，Migration 文件进入 Git。
        // 如果用 DateTime.UtcNow，每次生成 Migration 时时间都不一样，
        // 导致 Migration 文件内容每次都变，Git 历史会被污染。
        // 固定时间保证 Migration 文件的内容是稳定的、可重复的。
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        // 使用预先计算好的固定 hash，而不是在这里动态生成
        // 原因：PasswordHasher 每次调用会用随机 salt，导致 EF Core 9 每次
        // 扫描模型时发现值不同，误判为"有未提交的变更"
        const string adminPasswordHash = "AQAAAAIAAYagAAAAEHCDDS0DL8S0+v4RJ8Xbotyu+8ZvjCEg7EODicW0j/KfCC6beDga+cX+eYM2TkRiRg==";

        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@uucars.com",
            PasswordHash = adminPasswordHash,
            Role = UserRole.Admin,
            EmailConfirmed = true,
            CreatedAt = seedDate,
            UpdatedAt = seedDate
        };

        // 添加种子数据给User实体
        modelBuilder.Entity<User>().HasData(adminUser);
    }
}