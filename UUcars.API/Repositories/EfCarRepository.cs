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
}