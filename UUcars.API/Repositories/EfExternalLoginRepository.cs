using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfExternalLoginRepository : IExternalLoginRepository
{
    private readonly AppDbContext _context;

    public EfExternalLoginRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExternalLogins
            .Include(login => login.User)
            .FirstOrDefaultAsync(
                login => login.Provider == provider &&
                         login.ProviderSubject == providerSubject,
                cancellationToken);
    }

    public async Task<ExternalLogin> AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default)
    {
        _context.ExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync(cancellationToken);

        return externalLogin;
    }
}