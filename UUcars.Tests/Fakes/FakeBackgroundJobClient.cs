using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace UUcars.Tests.Fakes;

/// <summary>
///     测试用的 BackgroundJobClient：记录入队的任务，不真正执行
/// </summary>
public class FakeBackgroundJobClient : IBackgroundJobClient
{
    // 记录所有入队的任务（方便在测试里断言"是否入队了"）
    public List<string> EnqueuedJobs { get; } = new();

    public string Enqueue(Job job, IState state)
    {
        EnqueuedJobs.Add(job.Method.Name);
        return Guid.NewGuid().ToString(); // 返回一个假的 Job ID
    }

    public bool ChangeState(string jobId, IState state, string? expectedCurrentState)
    {
        return true;
    }

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job.Method.Name);
        return Guid.NewGuid().ToString();
    }
}