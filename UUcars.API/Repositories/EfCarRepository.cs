using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public class EfCarRepository:ICarRepository
{
    private readonly AppDbContext _context;
    public EfCarRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default)
    {
        _context.Add(car);
        await _context.SaveChangesAsync(cancellationToken);
        return car;
    }

    public async Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .Include(c => c.Seller)     // 同时加载 Seller 导航属性
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);
        return car;
    }

    public async Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // 构建基础查询（此时还没有执行 SQL，只是在构建 IQueryable）
        var query = _context.Cars
            .Include(c => c.Seller)
            .Where(c => c.Status == status);

        // CountAsync：单独执行一次 COUNT(*) 查询，获取符合条件的总数
        // 注意：这里用的是过滤后的 query，不是全表 COUNT
        var totalCount = await query.CountAsync(cancellationToken);

        // 在同一个 query 基础上加分页，执行第二次查询取数据
        // 两次查询共享同一个 WHERE 条件，保证 totalCount 和数据是一致的
        var cars = await query
            .OrderByDescending(c => c.CreatedAt)    // 按创建时间降序（最新发布的在前）
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // C# 元组语法：(变量名: 值, 变量名: 值)
        return (cars, totalCount);
    }
}