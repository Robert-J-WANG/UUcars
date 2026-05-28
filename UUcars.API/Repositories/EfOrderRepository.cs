using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public EfOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Car)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking() // ✅ 只读
            //.Include(o => o.Car)
            //.Include(o => o.Seller)
            .Where(o => o.BuyerId == buyerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new Order // ✅ 投影
            {
                Id = o.Id,
                CarId = o.CarId,
                BuyerId = o.BuyerId,
                SellerId = o.SellerId,
                Price = o.Price,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                // Car：只取列表展示需要的字段（不含 Description）
                Car = new Car
                {
                    Id = o.Car.Id,
                    Title = o.Car.Title,
                    Brand = o.Car.Brand,
                    Year = o.Car.Year,
                    Images = o.Car.Images
                        .OrderBy(i => i.SortOrder)
                        .Take(1)
                        .ToList()
                },
                // 买家查"我买的"，关心卖家是谁
                Seller = new User
                {
                    Id = o.Seller.Id,
                    Username = o.Seller.Username
                }
                // Buyer 不需要（就是自己）
            })
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking() // ✅ 只读
            //.Include(o => o.Car)
            //.Include(o => o.Buyer)
            .Where(o => o.SellerId == sellerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new Order // ✅ 投影
            {
                Id = o.Id,
                CarId = o.CarId,
                BuyerId = o.BuyerId,
                SellerId = o.SellerId,
                Price = o.Price,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                Car = new Car
                {
                    Id = o.Car.Id,
                    Title = o.Car.Title,
                    Brand = o.Car.Brand,
                    Year = o.Car.Year,
                    Images = o.Car.Images
                        .OrderBy(i => i.SortOrder)
                        .Take(1)
                        .ToList()
                },
                // 卖家查"我卖的"，关心买家是谁
                Buyer = new User
                {
                    Id = o.Buyer.Id,
                    Username = o.Buyer.Username
                }
                // Seller 不需要（就是自己）
            })
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }
}