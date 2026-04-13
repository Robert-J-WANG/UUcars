using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public EfUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // FirstOrDefaultAsync：找到第一条匹配的记录，没有则返回 null
        // 用 ToLower() 做大小写不敏感的邮箱匹配
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // 把实体加入 DbContext 的追踪（此时还没写数据库）
        _context.Users.Add(user);

        // SaveChangesAsync：把所有待处理的变更一次性写入数据库
        // 执行完之后，user.Id 会被数据库分配的自增值填充
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
    
    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        /*
        EF Core 的变更追踪机制：
        从 DbContext 查出来的实体会被 EF Core "追踪"。
        修改它的属性后调用 SaveChangesAsync，EF Core 自动检测哪些字段变了，
        只对变化的字段生成 UPDATE 语句，不需要手动写 SQL。
        这里的 user 对象是从 GetByIdAsync 查出来的，已经在追踪中，
        直接改属性再 Update + SaveChanges 就行
        */
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}