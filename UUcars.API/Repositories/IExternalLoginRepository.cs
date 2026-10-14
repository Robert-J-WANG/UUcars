using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IExternalLoginRepository
{
    // 根据 Google 用户编号查找对应的 UUcars 登录记录
    Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default);

    // 保存一条 Google 账号和 UUcars 用户的关联记录
    Task<ExternalLogin> AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default);
}