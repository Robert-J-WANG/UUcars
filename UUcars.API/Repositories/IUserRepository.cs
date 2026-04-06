using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    // 按 Email 查找用户（登录和注册校验时使用）
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // 新增用户，返回创建后的用户（含数据库分配的 Id）
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    
    // 新增：按 Id 查询用户
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}