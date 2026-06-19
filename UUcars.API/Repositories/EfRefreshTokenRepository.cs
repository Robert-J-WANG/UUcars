using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public EfRefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            // Include User 是因为后续需要用 user.Id 签发新的 AccessToken
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        // EF Core 7+ 批量更新，不需要先 Select 再逐条修改
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.IsRevoked, true),
                cancellationToken);
    }
}