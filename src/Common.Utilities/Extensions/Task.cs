using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.Extensions;

/// <summary>
/// Extension methods for <see cref="Task"/>.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Observes the task to avoid the UnobservedTaskException event to be raised.
    /// </summary>
    public static void Forget(this Task task)
    {
        // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
        // Only care about tasks that may fault (not completed) or are faulted,
        // so fast-path for SuccessfullyCompleted and Canceled tasks.
        if (!task.IsCompleted || task.IsFaulted)
        {
            // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
            // https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards?WT.mc_id=DT-MVP-5003978#a-standalone-discard
            _ = ForgetAwaited(task);
        }
    }

    // Allocate the async/await state machine only when needed for performance reasons.
    // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
    private static async Task ForgetAwaited(Task task)
    {
        try
        {
            // No need to resume on the original SynchronizationContext
            await task.ConfigureAwait(ConfigureAwaitOptions.None);
        }
        catch
        {
            // Nothing to do here
        }
    }

    /// <summary>
    /// Runs <paramref name="taskFactory"/> on the current thread.
    /// </summary>
    /// <param name="taskFactory">A method to create the task to run.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static void RunSync(this Func<Task?> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory, nameof(taskFactory));
        var previousContext = SynchronizationContext.Current;
        var newContext = new SingleThreadSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(newContext);
            newContext.OperationStarted();
            var task = taskFactory();
            if (task == null)
            {
                newContext.OperationCompleted();
            }
            else
            {
                task.ContinueWith(_ => newContext.OperationCompleted(), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
                newContext.RunOnCurrentThread();
                task.GetAwaiter().GetResult();
            }
        }
        finally
        {
            newContext.Dispose();
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    /// <summary>
    /// Runs <paramref name="taskFactory"/> on the current thread.
    /// </summary>
    /// <param name="taskFactory">A method to create the task to run.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static T? RunSync<T>(this Func<Task<T?>?> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory, nameof(taskFactory));
        var previousContext = SynchronizationContext.Current;
        var newContext = new SingleThreadSynchronizationContext();

        try
        {
            SynchronizationContext.SetSynchronizationContext(newContext);
            newContext.OperationStarted();
            var task = taskFactory();
            if (task == null)
            {
                newContext.OperationCompleted();
                return default;
            }
            else
            {
                task.ContinueWith(_ => newContext.OperationCompleted(), cancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
                newContext.RunOnCurrentThread();
                return task.GetAwaiter().GetResult();
            }
        }
        finally
        {
            newContext.Dispose();
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    internal sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object?>> _queue = [];

        private int _operationCount;
        public void Dispose() => _queue.Dispose();

        public override SynchronizationContext CreateCopy() => this;

        public override void OperationStarted() => Interlocked.Increment(ref _operationCount);

        public override void OperationCompleted()
        {
            if (Interlocked.Decrement(ref _operationCount) == 0)
            {
                _queue.CompleteAdding();
            }
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            ArgumentNullException.ThrowIfNull(d, nameof(d));
            _queue.Add(new KeyValuePair<SendOrPostCallback, object?>(d, state));
        }

        public override void Send(SendOrPostCallback d, object? state)
            => throw new NotSupportedException("Send is not supported.");

        internal void RunOnCurrentThread()
        {
            foreach (var workItem in _queue.GetConsumingEnumerable())
            {
                workItem.Key(workItem.Value);
            }
        }
    }
}
