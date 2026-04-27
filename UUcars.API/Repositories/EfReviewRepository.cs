using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public EfReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByOrderIdAsync(int orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .AnyAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task<Review> AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<List<Review>> GetByRevieweeIdAsync(int revieweeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reviews.Include(r => r.Reviewer).Where(r => r.RevieweeId == revieweeId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }
}