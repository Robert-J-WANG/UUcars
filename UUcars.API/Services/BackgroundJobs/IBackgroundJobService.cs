namespace UUcars.API.Services.BackgroundJobs;

public interface IBackgroundJobService
{
    void EnqueueEmailVerification(string email, string token);

    void EnqueuePasswordReset(string email, string token);

    void RegisterRecurringJobs();
}
