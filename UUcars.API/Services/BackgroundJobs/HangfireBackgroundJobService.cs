using Hangfire;
using UUcars.API.Services.Email;
using UUcars.API.Services.Hangfire;

namespace UUcars.API.Services.BackgroundJobs;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireBackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public void EnqueueEmailVerification(string email, string token)
    {
        _backgroundJobClient.Enqueue<IEmailService>(service =>
            service.SendEmailVerificationAsync(
                email,
                token,
                CancellationToken.None));
    }

    public void EnqueuePasswordReset(string email, string token)
    {
        _backgroundJobClient.Enqueue<IEmailService>(service =>
            service.SendPasswordResetAsync(
                email,
                token,
                CancellationToken.None));
    }

    public void RegisterRecurringJobs()
    {
        _recurringJobManager.AddOrUpdate<TokenCleanupService>(
            "cleanup-expired-tokens",
            service => service.CleanExpiredTokensAsync(),
            Cron.Daily(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }
}
