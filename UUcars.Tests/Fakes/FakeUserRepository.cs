using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

// 用内存字典模拟数据库，实现 IUserRepository 接口
// 只用于测试，不会真正操作数据库
public class FakeUserRepository : IUserRepository
{
    // 模拟数据库表，key = Email（方便按邮箱查找）
    private readonly Dictionary<string, User> _store = new();

    // 供测试用：预先插入数据，模拟"数据库里已有这条记录"
    public void Seed(User user) => _store[user.Email.ToLower()] = user;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(email.ToLower(), out var user);
        return Task.FromResult(user);
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // 模拟数据库自增 Id
        user.Id = _store.Count + 1;
        _store[user.Email.ToLower()] = user;
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
       var user= _store.Values.FirstOrDefault(u => u.Id == id);
       return Task.FromResult(user);
    }
}