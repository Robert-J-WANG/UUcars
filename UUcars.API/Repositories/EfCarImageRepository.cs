using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfCarImageRepository : ICarImageRepository
{
    private readonly AppDbContext _context;

    public EfCarImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        _context.CarImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);
        return image;
    }

    public async Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default)
    {
        return await _context.CarImages
            .Include(ci => ci.Car)  // 加载关联的 Car，用于后续验证车主和状态
            .FirstOrDefaultAsync(ci => ci.Id == imageId, cancellationToken);
    }

    public async Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        // 图片是真正的物理删除——图片记录本身没有业务含义，删了就是删了
        // 和车辆的逻辑删除不同：车辆有关联订单等历史数据需要保留，
        // 图片只是附属资源，删掉不影响任何业务记录
        _context.CarImages.Remove(image);
        await _context.SaveChangesAsync(cancellationToken);
    }
}