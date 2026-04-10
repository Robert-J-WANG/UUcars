using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public class EfCarRepository : ICarRepository
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
            .Include(c => c.Seller) // 同时加载 Seller 导航属性
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
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // 构建基础查询（此时还没有执行 SQL，只是在构建 IQueryable）
        // 从 status 过滤开始构建 IQueryable
        // 后续每一个 Where 都是在已有的查询基础上追加条件，
        // 最终 EF Core 会把所有条件合并成一条 SQL 语句
        var query = _context.Cars
            .Include(c => c.Seller)
            .Where(c => c.Status == status);

        // 品牌过滤：只有传了 Brand 才追加这个条件
        // string.IsNullOrWhiteSpace：同时处理 null、空字符串、纯空格的情况
        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            // Contains + ToLower：大小写不敏感的模糊匹配
            // 比如搜 "bmw" 能匹配 "BMW"、"Bmw"
            // EF Core 会把这个翻译成 SQL 的 LIKE '%bmw%'（在 SQL Server 里不区分大小写）
            query = query.Where(c => c.Brand.Contains(request.Brand.ToLower()));
        }

        // 价格区间：MinPrice 和 MaxPrice 各自独立，可以只传其中一个
        if (request.MinPrice.HasValue)
        {
            query = query.Where(c => c.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= request.MaxPrice.Value);
        }

        if (request.MinYear.HasValue)
        {
            query = query.Where(c => c.Year >= request.MinYear.Value);
        }

        if (request.MaxYear.HasValue)
        {
            query = query.Where(c => c.Year <= request.MaxYear.Value);
        }


        // 到这里 query 还是 IQueryable，所有 Where 条件都还没有执行 SQL
        // CountAsync 触发第一次数据库查询：SELECT COUNT(*) WHERE ...（含所有过滤条件）
        var totalCount = await query.CountAsync(cancellationToken);

        // 在同一个 query 基础上加分页，执行第二次查询取数据
        // 两次查询共享同一个 WHERE 条件，保证 totalCount 和数据是一致的

        var page = request.Page;
        var pageSize = Math.Min(50, request.PageSize);

        var cars = await query
            .OrderByDescending(c => c.CreatedAt) // 按创建时间降序（最新发布的在前）
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // C# 元组语法：(变量名: 值, 变量名: 值)
        return (cars, totalCount);
    }

    public async Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Cars
            .Include(c => c.Seller)
            .Where(c => c.SellerId == sellerId);
        // 注意：这里没有过滤 Status，返回卖家所有状态的车辆
        // 但排除逻辑删除的车辆——卖家也不需要看到已删除的车
        // 如果将来需要显示已删除的车，可以单独加一个接口

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            query = query.Where(c => c.Brand.Contains(request.Brand.ToLower()));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(c => c.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= request.MaxPrice.Value);
        }

        if (request.MinYear.HasValue)
        {
            query = query.Where(c => c.Year >= request.MinYear.Value);
        }

        if (request.MaxYear.HasValue)
        {
            query = query.Where(c => c.Year <= request.MaxYear.Value);
        }

        // 去掉逻辑删除的车（卖家视角也不需要看到已删除的车）
        query = query.Where(c => c.Status != CarStatus.Deleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = request.Page;
        var pageSize = Math.Min(50, request.PageSize);

        var cars = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (cars, totalCount);
    }


    public async Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .Include(c => c.Seller)
            .Include(c => c.Images) // 同时加载图片列表
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}