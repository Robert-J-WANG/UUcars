using Microsoft.EntityFrameworkCore;
using UUcars.API.Entities;

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
    }
}