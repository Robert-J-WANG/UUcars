using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

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
}