using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.Common.Utilities.UnitTests.Extensions;

public class TaskSpecs
{
    [Fact]
    public void Forget_Should_Not_Fail()
    {
        Task.CompletedTask.Forget();
        Task.Delay(TimeSpan.FromMilliseconds(1)).Forget();

        ThrowExceptionAsync(TimeSpan.Zero).Forget();
        ThrowExceptionAsync(TimeSpan.FromMilliseconds(2)).Forget();

        Thread.Sleep(TimeSpan.FromMilliseconds(5));

        Assert.True(true);
    }

    private static async Task ThrowExceptionAsync(TimeSpan delay)
    {
        if (delay.TotalMilliseconds > 0)
        {
            await Task.Delay(delay);
        }

        throw new ApplicationException("Fail!");
    }
}
