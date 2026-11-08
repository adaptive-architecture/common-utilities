using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
    public void Forget_Should_Observe_Task_Exceptions()
    {
        var unobserved = new ConcurrentBag<string>();
        void OnUnobserved(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var ex in e.Exception.Flatten().InnerExceptions)
            {
                unobserved.Add(ex.Message);
            }
        }

        var marker = Guid.NewGuid().ToString("N");
        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            CreateFaultedTasks(marker, forget: true);
            CreateFaultedTasks(marker + "-control", forget: false);
            CollectGarbage();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }

        // The control tasks prove the detection works in this environment.
        Assert.Contains(marker + "-control", unobserved);
        Assert.DoesNotContain(marker, unobserved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFaultedTasks(string message, bool forget)
    {
        var alreadyFaulted = Task.FromException(new ApplicationException(message));
        var genericFaulted = Task.FromException<int>(new ApplicationException(message));
        var pending = new TaskCompletionSource();

        if (forget)
        {
            alreadyFaulted.Forget();
            genericFaulted.Forget();
            pending.Task.Forget();
        }

        pending.SetException(new ApplicationException(message));
    }

    private static void CollectGarbage()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
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
    public void Task_Void_RunSync_Should_Propagate_Cancellation()
    {
        Func<Task> taskFactory = static () => Task.FromCanceled(new CancellationToken(canceled: true));
        _ = Assert.Throws<TaskCanceledException>(() => taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Task_Value_RunSync_Should_Propagate_Cancellation()
    {
        Func<Task<bool>> taskFactory = static () => Task.FromCanceled<bool>(new CancellationToken(canceled: true));
        _ = Assert.Throws<TaskCanceledException>(() => _ = taskFactory.RunSync(cancellationToken: TestContext.Current.CancellationToken));
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
