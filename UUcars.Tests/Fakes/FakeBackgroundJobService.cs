using UUcars.API.Services.BackgroundJobs;

namespace UUcars.Tests.Fakes;

public class FakeBackgroundJobService : IBackgroundJobService
{
    public List<string> EnqueuedJobs { get; } = [];

    public bool RecurringJobsRegistered { get; private set; }

    public void EnqueueEmailVerification(string email, string token)
    {
        EnqueuedJobs.Add(nameof(EnqueueEmailVerification));
    }

    public void EnqueuePasswordReset(string email, string token)
    {
        EnqueuedJobs.Add(nameof(EnqueuePasswordReset));
    }

    public void RegisterRecurringJobs()
    {
        RecurringJobsRegistered = true;
    }
}
