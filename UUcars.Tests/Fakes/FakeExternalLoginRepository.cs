using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeExternalLoginRepository
    : IExternalLoginRepository
{
    // 用内存集合保存 Google 身份与 UUcars User 的关联记录。
    private readonly List<ExternalLogin> _store = [];

    public Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        var externalLogin = _store.FirstOrDefault(item =>
            item.Provider == provider &&
            item.ProviderSubject == providerSubject);

        return Task.FromResult(externalLogin);
    }

    public Task<ExternalLogin> AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default)
    {
        externalLogin.Id = _store.Count + 1;
        _store.Add(externalLogin);

        return Task.FromResult(externalLogin);
    }

    public void Seed(ExternalLogin externalLogin)
    {
        // 预先放入已有的关联记录，用于模拟老用户再次 Google 登录。
        _store.Add(externalLogin);
    }

    public IReadOnlyList<ExternalLogin> Items => _store;
}
