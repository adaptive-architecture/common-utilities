using AdaptArch.Common.Utilities.Extensions;
using SingleThreadSynchronizationContext = AdaptArch.Common.Utilities.Extensions.TaskExtensions.SingleThreadSynchronizationContext;

namespace AdaptArch.Common.Utilities.UnitTests.Extensions;
#pragma warning disable S2925 // SONAR: Do not use 'Thread.Sleep()' in a test.

public class TaskSpecs
{
    [Fact]
    public void Forget_Should_Not_Fail()
    {
        Task.CompletedTask.Forget();
        Task.Delay(TimeSpan.FromMilliseconds(1), TestContext.Current.CancellationToken).Forget();

        ThrowExceptionAsync(TimeSpan.Zero).Forget();
        ThrowExceptionAsync(TimeSpan.FromMilliseconds(2)).Forget();

        Thread.Sleep(TimeSpan.FromMilliseconds(5));

        Assert.True(true);
    }

    [Fact]
    public void Task_Void_RunSync_Should_Allow_Running_Task_Synchronously()
    {
        Func<Task> taskFactory = () => Task.CompletedTask;
        taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);

        taskFactory = () => Task.Delay(TimeSpan.FromMilliseconds(1));
        taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);

        taskFactory = GetNullTask;
        taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);

        taskFactory = () => ThrowExceptionAsync(TimeSpan.FromMilliseconds(2));
        _ = Assert.Throws<ApplicationException>(() => taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(true);
    }

    [Fact]
    public void Task_Value_RunSync_Should_Allow_Running_Task_Synchronously()
    {
        var result = false;
        Func<Task<bool>> taskFactory = () => Task.FromResult(true);
        result = taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);

        taskFactory = delegate
        {
            return Task.FromResult(false);
        };
        result = taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);

        result = true;
        Assert.True(result);
        taskFactory = GetNullTask<bool>;
        result = taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);

        taskFactory = async delegate
        {
            await ThrowExceptionAsync(TimeSpan.FromMilliseconds(2));
            return true;
        };
        _ = Assert.Throws<ApplicationException>(() => taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(true);
    }

    [Fact]
    public void SingleThreadSynchronizationContext_Should_Return_The_Same_Instance_When_Cloning()
    {
        var context = new SingleThreadSynchronizationContext();
        var clone = context.CreateCopy();
        Assert.Same(context, clone);
    }

    [Fact]
    public void SingleThreadSynchronizationContext_Should_Throw_When_Sending()
    {
        var context = new SingleThreadSynchronizationContext();
        _ = Assert.Throws<NotSupportedException>(() => context.Send(_ => { }, null));
    }

    private static async Task ThrowExceptionAsync(TimeSpan delay)
    {
        if (delay.TotalMilliseconds > 0)
        {
            await Task.Delay(delay);
        }

        throw new ApplicationException("Fail!");
    }

    private static Task GetNullTask()
    {
        return null!;
    }

    private static Task<T> GetNullTask<T>()
    {
        return null!;
    }
}
